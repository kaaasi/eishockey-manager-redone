#!/usr/bin/env python3
"""Function map of em.exe and its coverage by the remake (track `re-abdeckung`).

Usage:
    python3 tools/reverse-engineering/funktionskarte.py <path/to/em.exe> [--repo .] [--write] [--csv out.csv]

Without --write the generated Markdown is printed to stdout. With --write the block between the
markers in docs/re-abdeckung.md is replaced (hand-written text outside the markers stays).
Needs `ndisasm` (nasm). Reads em.exe only; nothing from the original files is written except
addresses, sizes and short text snippets used as hints (no code, no graphics).

Steps:
 1. Layout: MZ header, Borland segment table at file 0x163C0 (8 bytes per entry: segment, size, flags),
    base code segments (flag 1) and VROOMM overlay stubs (flag 3). Stub header +4 = overlay offset
    relative to file 0x255C0, +8 = code size; entries from +0x20, 5 bytes each (INT 3Fh, word at +2 =
    offset in the overlay).
 2. Function starts: stub entries, prologs (push bp / mov bp,sp; enter), near and far call targets.
    A function ends where the next one starts (same segment/overlay).
 3. Coverage: addresses in src/**/*.cs comment blocks ([ORIGINAL]/[APPROX]) and docs/**/*.md (except
    docs/archiv/, docs/reviews/ and the generated docs/re-abdeckung.md) are mapped onto the functions.
    Values up to the DGROUP size are ambiguous (file offset or DGROUP offset). They count as file offsets only
    with a file keyword in the same sentence, in a doc that declares all addresses as file offsets, or as an
    exact function start - never in brackets ("[0x5CF3]"), after "DGROUP" or on a line that names DGROUP values.
    A stub-entry call ("0x118:0x70") under [ORIGINAL] only says that the call is original: if another hit
    marks the same function [APPROX] and only calls back the "umgesetzt", the function counts as "genähert".
 4. Hints: DGROUP strings (plain and XOR 0xD9/0xC9), image numbers passed to the kb\\ loaders, globals,
    callers.
"""

from __future__ import annotations

import argparse
import bisect
import csv
import re
import struct
import subprocess
import sys
import tempfile
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

SEG_TABLE = 0x163C0
OVERLAY_BASE = 0x255C0
HEADER = 0x1E00
DGROUP_SEG = 0x14FD
DGROUP_FILE = HEADER + DGROUP_SEG * 16
DGROUP_SIZE = 0xE75A
MIN_LIST_SIZE = 64

# Segment names used by the old Ghidra analysis (docs/reverse-engineering.md) -> stub selector.
# Derived from the load order (each overlay follows the previous one, rounded to paragraphs);
# checked against 3810:4D3B, 66C3:0730 and 5E73 = stub 0x130 (docs/spielberechnung.md).
GHIDRA_OVERLAY = {
    0x337B: 0xC0, 0x33FB: 0xC8, 0x34FF: 0xD0, 0x3606: 0xD8, 0x3810: 0xE0, 0x3D11: 0xE8,
    0x42E9: 0xF0, 0x4711: 0xF8, 0x4A4C: 0x100, 0x4D30: 0x108, 0x502F: 0x110, 0x52F7: 0x118,
    0x57D6: 0x120, 0x59E3: 0x128, 0x5E73: 0x130, 0x63A2: 0x138, 0x66C3: 0x140,
}
STUB_TO_GHIDRA = {v: k for k, v in GHIDRA_OVERLAY.items()}

STATUS_ORDER = ["umgesetzt", "genähert", "dokumentiert", "unbekannt"]

# Role of the base segments (assumption from interrupts, port I/O, strings and callers; see docs/re-abdeckung.md).
# "System" segments are platform layers (replaced by .NET/MonoGame) and are left out of the gap lists.
BASE_KIND = {
    0x0: ("System", "C-Laufzeit (Borland C++ 1991: Copyright, INT 21h, rand/LXMUL/LDIV)"),
    0x238: ("Spiel", "Spiellogik Basis (Saisonende, Statistik)"),
    0x46E: ("Spiel", "Spiellogik Basis (Training, Spieler)"),
    0x62C: ("Spiel", "Spiellogik Basis (Spielberechnung, Zufall)"),
    0x902: ("Spiel", "Fensterverwaltung („Zu viele Fenster auf !“)"),
    0x99F: ("Spiel", "Bilder laden (kb\\…vga), Texte, Geldausgabe"),
    0xA99: ("System", "Speicher (EMS INT 67h, XMS INT 2Fh), Datei"),
    0xBBF: ("System", "Grafik und Maus (VGA-Ports, INT 10h, INT 33h)"),
    0x1115: ("System", "Dateizugriff (Hüllen um die C-Laufzeit)"),
    0x11FD: ("System", "Timer und Interrupts (PIC 0x20/0x21, PIT 0x40/0x43)"),
    0x125B: ("System", "Sound Blaster digital (DMA-Ports 0x02–0x0C)"),
    0x12F7: ("System", "FM-Treiber SBFMDRV (CmfPlayer.cs), Tastatur (Port 0x60, INT 16h)"),
}


def is_system(r: "Region") -> bool:
    return r.seg is not None and BASE_KIND.get(r.seg, ("Spiel",))[0] == "System"


@dataclass
class Region:
    """A code area: base segment or overlay."""
    name: str          # "Basis 0x46E" or "Overlay 0x130"
    key: str           # "B046E" / "O0130" (sort key)
    start: int         # file offset
    size: int
    seg: int | None = None       # base segment value
    stub: int | None = None      # stub selector
    stub_seg: int | None = None  # raw segment of the stub
    entries: list[int] = field(default_factory=list)  # overlay offsets of stub entries

    @property
    def end(self) -> int:
        return self.start + self.size


@dataclass
class Function:
    start: int
    end: int
    region: Region
    reasons: set[str] = field(default_factory=set)
    refs: dict[str, list[str]] = field(default_factory=lambda: defaultdict(list))
    strings: list[str] = field(default_factory=list)
    images: set[int] = field(default_factory=set)
    globals_: set[int] = field(default_factory=set)
    callers: set[int] = field(default_factory=set)
    callees: set[int] = field(default_factory=set)
    call_refs: set[str] = field(default_factory=set)  # "umgesetzt" hits that only name a call (stub entry)
    entry: int | None = None   # stub entry offset (0x20 + 5k) if exported

    @property
    def size(self) -> int:
        return self.end - self.start

    @property
    def status(self) -> str:
        done = self.refs.get("umgesetzt")
        if done and self.refs.get("genähert") and all(w in self.call_refs for w in done):
            return "genähert"  # call named as original, the function itself only approximated
        for s in STATUS_ORDER[:3]:
            if self.refs.get(s):
                return s
        return "unbekannt"

    def address(self) -> str:
        r = self.region
        if r.stub is not None:
            return f"0x{self.start:05X} (Ov {r.stub:#x}+0x{self.start - r.start:X})"
        return f"0x{self.start:05X} ({r.seg:X}:{self.start - r.start:04X})"


# ---------------------------------------------------------------- layout

def read_layout(exe: bytes) -> list[Region]:
    entries = []
    for i in range(0x60):
        seg, size, flags, _ = struct.unpack_from("<HHHH", exe, SEG_TABLE + i * 8)
        entries.append((seg, size, flags))
        if seg == DGROUP_SEG:
            break
    regions: list[Region] = []
    seen = set()
    for i, (seg, size, flags) in enumerate(entries):
        if flags == 1 and size and seg not in seen:
            seen.add(seg)
            regions.append(Region(f"Basis 0x{seg:X}", f"B{seg:04X}", HEADER + seg * 16, size, seg=seg))
        elif flags == 3:
            stub = HEADER + seg * 16
            ov_off = struct.unpack_from("<I", exe, stub + 4)[0]
            code_size = struct.unpack_from("<H", exe, stub + 8)[0]
            n = (size - 0x20) // 5
            offs = [struct.unpack_from("<H", exe, stub + 0x20 + k * 5 + 2)[0] for k in range(n)]
            regions.append(Region(f"Overlay 0x{i * 8:X}", f"O{i * 8:04X}", OVERLAY_BASE + ov_off, code_size,
                                  stub=i * 8, stub_seg=seg, entries=offs))
    return regions


# ---------------------------------------------------------------- disassembly

INSN = re.compile(r"^([0-9A-F]{8})\s+([0-9A-F]+)\s+(.*)$")


def disassemble(exe: bytes, region: Region, sync: list[int] | None = None) -> list[tuple[int, bytes, str]]:
    """Linear sweep with ndisasm; `sync` = region offsets where decoding must restart (known function starts)."""
    with tempfile.NamedTemporaryFile(suffix=".bin") as tmp:
        tmp.write(exe[region.start:region.end])
        tmp.flush()
        cmd = ["ndisasm", "-b16", "-o0"]
        for off in sorted(set(sync or [])):
            cmd += ["-s", str(off)]
        out = subprocess.run(cmd + [tmp.name], capture_output=True, text=True, check=True).stdout
    result = []
    for line in out.splitlines():
        m = INSN.match(line)
        if m:
            result.append((int(m.group(1), 16), bytes.fromhex(m.group(2)), m.group(3)))
    return result


FAR_CALL = re.compile(r"^(call|jmp) (?:word )?0x([0-9a-f]+):0x([0-9a-f]+)$")
NEAR_CALL = re.compile(r"^call 0x([0-9a-f]+)$")
IMM = re.compile(r"^(push|mov) (?:word )?(?:[a-z]{2}|word \[[^\]]+\]|\[[^\]]+\])?,?(?:word )?0x([0-9a-f]+)$")
MEM = re.compile(r"\[(?:(?:[a-z]{2})(?:\+[a-z]{2})?)?([+-])?0x([0-9a-f]+)\]")


class Analyzer:
    def __init__(self, exe: bytes):
        self.exe = exe
        self.regions = read_layout(exe)
        self.by_seg = {r.seg: r for r in self.regions if r.seg is not None}
        self.by_stub = {r.stub: r for r in self.regions if r.stub is not None}
        self.by_stub_seg = {r.stub_seg: r for r in self.regions if r.stub is not None}
        self.code: dict[str, list[tuple[int, bytes, str]]] = {}
        self.functions: list[Function] = []
        self.starts: list[int] = []

    # resolve a far pointer seen in region `src` to a file offset
    def resolve_far(self, src: Region, seg: int, off: int) -> int | None:
        if src.stub is not None and seg % 8 == 0 and seg <= 0x148:
            entry = self._table(seg // 8)
            if entry is None:
                return None
            raw_seg, flags = entry
            if flags == 3:
                return self._stub_entry(self.by_stub.get(seg), off)
            if flags == 1 and raw_seg in self.by_seg:
                return self.by_seg[raw_seg].start + off
            return None
        if seg in self.by_seg:
            return self.by_seg[seg].start + off
        if seg in self.by_stub_seg:
            return self._stub_entry(self.by_stub_seg[seg], off)
        return None

    def _table(self, index: int):
        seg, _, flags, _ = struct.unpack_from("<HHHH", self.exe, SEG_TABLE + index * 8)
        return seg, flags

    @staticmethod
    def _stub_entry(region: Region | None, entry_off: int) -> int | None:
        if region is None or entry_off < 0x20 or (entry_off - 0x20) % 5:
            return None
        k = (entry_off - 0x20) // 5
        if k >= len(region.entries):
            return None
        return region.start + region.entries[k]

    def region_of(self, addr: int) -> Region | None:
        for r in self.regions:
            if r.start <= addr < r.end:
                return r
        return None

    def run(self) -> None:
        starts: dict[int, set[str]] = defaultdict(set)
        for r in self.regions:
            # segments are paragraph aligned: the first bytes may still belong to the previous segment
            first = max([r.start] + [o.end for o in self.regions if o is not r and o.start <= r.start < o.end])
            starts[first].add("Segmentanfang")
            for off in r.entries:
                starts[r.start + off].add("Stub")
            # prologs found in the raw bytes (a linear sweep loses sync after jump tables between functions)
            blob = self.exe[r.start:r.end]
            for m in re.finditer(rb"\x55\x8b\xec", blob):  # push bp; mov bp,sp (no ENTER in em.exe)
                starts[r.start + m.start()].add("Prolog")
        # iterate: disassemble with the known starts as sync points, add call targets, repeat
        for _ in range(4):
            before = len(starts)
            calls: list[tuple[int, int]] = []   # (call site, target)
            near_targets: dict[int, int] = defaultdict(int)
            for r in self.regions:
                sync = [a - r.start for a in starts if r.start <= a < r.end]
                code = disassemble(self.exe, r, sync)
                self.code[r.key] = code
                for off, raw, text in code:
                    addr = r.start + off
                    m = NEAR_CALL.match(text)
                    if m and raw[0] == 0xE8:
                        tgt = int(m.group(1), 16)
                        if tgt < r.size:
                            near_targets[r.start + tgt] += 1
                            calls.append((addr, r.start + tgt))
                        continue
                    m = FAR_CALL.match(text)
                    if m and raw[0] == 0x9A:
                        tgt = self.resolve_far(r, int(m.group(2), 16), int(m.group(3), 16))
                        if tgt is not None and self.region_of(tgt):
                            starts[tgt].add("Aufruf")
                            calls.append((addr, tgt))
            # near calls may come from mis-decoded data: accept a target with a prolog or at least two callers
            for tgt, n in near_targets.items():
                if tgt in starts or n >= 2:
                    starts[tgt].add("Aufruf")
            if len(starts) == before:
                break
        calls = [(site, tgt) for site, tgt in calls if tgt in starts]
        # build functions
        ordered = sorted(a for a in starts if self.region_of(a))
        funcs = []
        for idx, a in enumerate(ordered):
            reg = self.region_of(a)
            nxt = ordered[idx + 1] if idx + 1 < len(ordered) else reg.end
            end = min(nxt, reg.end)
            f = Function(a, end, reg, reasons=starts[a])
            if reg.stub is not None and a - reg.start in reg.entries:
                f.entry = 0x20 + 5 * reg.entries.index(a - reg.start)
            funcs.append(f)
        self.functions = funcs
        self.starts = [f.start for f in funcs]
        for site, tgt in calls:
            caller, callee = self.func_at(site), self.func_at(tgt)
            if caller and callee and caller is not callee:
                caller.callees.add(callee.start)
                callee.callers.add(caller.start)
        self._collect_operands()

    def func_at(self, addr: int) -> Function | None:
        i = bisect.bisect_right(self.starts, addr) - 1
        if i >= 0 and self.functions[i].start <= addr < self.functions[i].end:
            return self.functions[i]
        return None

    # ---------------------------------------------------------- strings, globals, images
    def dgroup_string(self, off: int) -> str | None:
        if off < 0x40 or off >= DGROUP_SIZE:  # small immediates are counters, not string offsets
            return None
        base = DGROUP_FILE + off
        if self.exe[base - 1] != 0:
            return None
        end = self.exe.find(b"\0", base, base + 120)
        if end < 0:
            return None
        raw = self.exe[base:end]
        if len(raw) >= 3 and all(0x20 <= c < 0x7F for c in raw) and sum(c >= 0x41 for c in raw) >= 2:
            return decode(raw)
        for key in (0xD9, 0xC9):
            dec = bytes(c ^ key for c in raw)
            if len(raw) >= 3 and all(0x20 <= c < 0x7F for c in dec) and sum(c >= 0x41 for c in dec) >= 2 \
                    and not all(0x20 <= c < 0x7F for c in raw):
                return decode(dec) + f" (XOR {key:#x})"
        return None

    def _collect_operands(self) -> None:
        kb_strings = {i for i in range(DGROUP_SIZE - 4)
                      if self.exe[DGROUP_FILE + i:DGROUP_FILE + i + 4] == b"kb\\\0"}
        loaders: set[int] = set()
        pushes: dict[int, list[tuple[int, int]]] = {}
        for r in self.regions:
            for off, raw, text in self.code[r.key]:
                addr = r.start + off
                f = self.func_at(addr)
                if f is None:
                    continue
                m = IMM.match(text)
                if m:
                    val = int(m.group(2), 16)
                    if val in kb_strings:
                        loaders.add(f.start)
                    s = self.dgroup_string(val)
                    if s and s not in f.strings:
                        f.strings.append(s)
                for sign, num in MEM.findall(text):
                    v = int(num, 16)
                    if sign == "-":
                        v = (-v) & 0xFFFF
                    if v >= 0x100 and "bp" not in text.split("[", 1)[1].split("]")[0]:
                        f.globals_.add(v)
        self.loaders = loaders
        self.all_globals = set().union(*(f.globals_ for f in self.functions))
        # strings reached through initialised far pointers in DGROUP (off, seg = 0x14FD), e.g. text tables
        for f in self.functions:
            for g in sorted(f.globals_):
                if g + 4 > DGROUP_SIZE:
                    continue
                off, seg = struct.unpack_from("<HH", self.exe, DGROUP_FILE + g)
                if seg == DGROUP_SEG:
                    s = self.dgroup_string(off)
                    if s and s not in f.strings:
                        f.strings.append(s)
        # image numbers: last immediate pushed before a call into a loader
        for r in self.regions:
            code = self.code[r.key]
            for i, (off, raw, text) in enumerate(code):
                tgt = None
                m = NEAR_CALL.match(text)
                if m and raw[0] == 0xE8:
                    tgt = r.start + int(m.group(1), 16)
                m = FAR_CALL.match(text)
                if m and raw[0] == 0x9A:
                    tgt = self.resolve_far(r, int(m.group(2), 16), int(m.group(3), 16))
                if tgt is None or tgt not in loaders:
                    continue
                f = self.func_at(r.start + off)
                for j in range(i - 1, max(i - 8, -1), -1):
                    t = code[j][2]
                    mm = re.match(r"^push (?:byte \+|word )?0x([0-9a-f]+)$", t)
                    if mm:
                        v = int(mm.group(1), 16)
                        if 0 < v < 300 and f is not None:
                            f.images.add(v)
                        break
                    mm = re.match(r"^mov a[xl],0x([0-9a-f]+)$", t)
                    if mm and j + 1 < i and code[j + 1][2] == "push ax":
                        v = int(mm.group(1), 16)
                        if 0 < v < 300 and f is not None:
                            f.images.add(v)
                        break


def decode(raw: bytes) -> str:
    return raw.decode("latin-1").replace("}", "ü").replace("{", "ä").replace("|", "ö").replace("~", "ß") \
        .replace("]", "Ü").replace("[", "Ä").replace("\\", "Ö") if not raw.startswith(b"kb\\") else raw.decode()


# ---------------------------------------------------------------- references

HEX = r"0x[0-9A-Fa-f]{3,5}"
RANGE = re.compile(rf"({HEX})`?\s*(?:–|-|bis)\s*`?({HEX})")
SEGOFF = re.compile(r"(?<![\w.:])(0x)?([0-9A-Fa-f]{2,4}):(0x)?([0-9A-Fa-f]{2,5})(?![\w:])")
CONSTANTS = {HEADER, SEG_TABLE, OVERLAY_BASE, DGROUP_FILE, 0x255B0}  # layout constants, not functions
BARE = re.compile(rf"(?<![\w:+]){HEX}(?![\w:])")
FILE_KEYWORDS = re.compile(r"Datei\s*`?0x|em\.exe|[Ff]ile\s*`?0x|Dateioffset|Datei-?[Oo]ffset")
STUBREF = re.compile(r"Stub(?:-Selektor)?\s+`?(0x[0-9A-Fa-f]+)`?[^\n]{0,40}?(Funktionsoffset|Stub-Offset|Einsprung|Offset)\s+`?(0x[0-9A-Fa-f]+)")
EXE_KEYWORDS = re.compile(r"em\.exe")
# docs about other binary files: "Datei 0x…" there is an offset in that file, not in em.exe
OTHER_FILE_DOCS = {"docs/formate/spielstand-original.md"}
# words that introduce a DGROUP variable when written right before a small value ("Liste `0x5D3B`")
DGROUP_WORDS = re.compile(r"(Liste|Tabellen?|Paarungen|Runde|Termin|Struktur|[Mm]anagerstruktur|Vereinsstruktur|"
                          r"Managerwert|Flag|Variable|Zustand)\s*`?$")
# DGROUP values that a comment calls "Datei" by mistake (checked against the bytes of em.exe)
DGROUP_MISLABELLED = {
    0x4B2E: "symbol table of the season end (DGROUP bytes 2, 1, 0, 5, 4, 6, 8; file 0x4B2E is code)",
}
SENTENCE_CUT = re.compile(r"\.\s|;|\n\s*(?:///?|\*)?\s*\n|\[(?:ORIGINAL|APPROX)\]|\|")
DGROUP_CTX = re.compile(r"(DGROUP|Feld|Spielerfeld|Vereinsfeld|Manager-Feld|Index|Globale|Zähler|Byte|Wort)\s*$")


class RefCollector:
    def __init__(self, an: Analyzer):
        self.an = an
        self.hits = 0
        self.file_keywords = FILE_KEYWORDS

    def resolve_segoff(self, seg: int, off: int) -> int | None:
        an = self.an
        if seg in GHIDRA_OVERLAY:
            r = an.by_stub[GHIDRA_OVERLAY[seg]]
            return r.start + off if off < r.size else None
        if seg in an.by_stub and seg >= 0xC0:
            r = an.by_stub[seg]
            a = an._stub_entry(r, off)
            if a is not None:
                return a
            return r.start + off if off < r.size else None
        for s in (seg, seg - 0x1000):
            if s in an.by_seg:
                r = an.by_seg[s]
                return r.start + off if off < r.size else None
        return None

    def resolve_stubref(self, stub: int, kind: str, off: int) -> int | None:
        an = self.an
        r = an.by_stub.get(stub)
        if r is None:
            r = next((x for x in an.by_stub.values() if HEADER + x.stub_seg * 16 == stub), None)
        if r is None:
            return None
        if kind in ("Stub-Offset", "Einsprung"):
            return an._stub_entry(r, off)
        return r.start + off if off < r.size else None

    def addresses(self, text: str, file_level: bool) -> list[tuple[int, int, int, bool]]:
        """Return (position, first, last, is_call) file offsets mentioned in `text`.

        is_call: the address is written as a stub entry (call target), not as an offset into the function."""
        out = []
        used = []
        for m in STUBREF.finditer(text):
            a = self.resolve_stubref(int(m.group(1), 16), m.group(2), int(m.group(3), 16))
            if a is not None:
                out.append((m.start(3), a, a, m.group(2) in ("Stub-Offset", "Einsprung")))
                used.append(m.span(1))
                used.append(m.span(3))
        for m in SEGOFF.finditer(text):
            seg_txt, off_txt = m.group(2), m.group(4)
            # skip clock times (0:08:09, 12:30) – addresses have a 0x prefix or a 3+ digit offset
            if not (m.group(1) or m.group(3)) and (len(off_txt) < 3 or (seg_txt + off_txt).isdigit() and len(off_txt) < 4):
                used.append(m.span())
                continue
            seg, off = int(seg_txt, 16), int(off_txt, 16)
            a = self.resolve_segoff(seg, off)
            if a is not None:
                is_call = seg >= 0xC0 and seg in self.an.by_stub and self.an._stub_entry(self.an.by_stub[seg], off) is not None
                out.append((m.start(), a, a, is_call))
            used.append(m.span())
        for m in RANGE.finditer(text):
            if any(s <= m.start() < e for s, e in used):
                continue
            a, b = int(m.group(1), 16), int(m.group(2), 16)
            if b > a and b - a < 0x3000 and self._accept(text, m.start(), a, file_level, rng=True):
                out.append((m.start(), a, b, False))
            used.append(m.span())
        for m in BARE.finditer(text):
            if any(s <= m.start() < e for s, e in used):
                continue
            a = int(m.group(0), 16)
            if self._accept(text, m.start(), a, file_level):
                out.append((m.start(), a, a, False))
        return out

    def _accept(self, text: str, pos: int, a: int, file_level: bool, rng: bool = False) -> bool:
        an = self.an
        if an.region_of(a) is None or a in CONSTANTS:
            return False
        before = text[max(0, pos - 24):pos]
        if DGROUP_CTX.search(before) or before.rstrip().endswith("+"):
            return False
        # DGROUP lists ("DGROUP 0x4861, Norm 0x48CD") until a sentence break
        seg_start = max(text.rfind(c, 0, pos) for c in ".;)\n")
        if "DGROUP" in text[seg_start + 1:pos] and not FILE_KEYWORDS.search(text[text.rfind("DGROUP", 0, pos):pos]):
            return False
        if a > DGROUP_SIZE:
            return True
        # ambiguous value (could be a DGROUP offset)
        if text[max(0, pos - 2):pos].rstrip(" `").endswith("["):
            return False  # "[0x5CF3]" = contents of a DGROUP variable
        if a in DGROUP_MISLABELLED or DGROUP_WORDS.search(before):
            return False  # a word like "Liste" introduces a DGROUP variable
        line = text[max(text.rfind("\n", 0, pos), text.rfind("|", 0, pos)) + 1:pos]  # line or table cell
        cuts = [m.end() for m in SENTENCE_CUT.finditer(text, 0, pos)]
        sentence = text[cuts[-1] if cuts else 0:pos]
        kws = [m.end() for m in self.file_keywords.finditer(sentence + text[pos:pos + 2])]  # "Datei 0x" ends in the number
        if kws and kws[-1] > sentence.rfind("DGROUP"):
            return True  # file keyword in the same sentence ("Datei 0x5B1A, 0x5CA0")
        if "DGROUP" in line:
            return False  # line names DGROUP values
        start = an.func_at(a) is not None and an.func_at(a).start == a and not rng
        if a in an.all_globals and not start:
            return False  # a DGROUP variable the code uses ("Liste 0x5D3B", "Tabelle 0x5CCB")
        if file_level:
            return True
        # otherwise only an exact function start (a keyword further up the comment does not count)
        return start

    def mark(self, first: int, last: int, status: str, where: str, is_call: bool = False) -> None:
        an = self.an
        i = bisect.bisect_right(an.starts, first) - 1
        i = max(i, 0)
        while i < len(an.functions) and an.functions[i].start <= last:
            f = an.functions[i]
            # a range that only touches the first bytes of the next function (end given inclusively or a little
            # too far, e.g. 0x5A9AE–0x5AA35) does not count for that function
            if f.end > first and (f.start <= first or f.start <= last - 3):
                if where not in f.refs[status]:
                    f.refs[status].append(where)
                    self.hits += 1
                    if is_call and f.start == first == last:
                        f.call_refs.add(where)
                elif not is_call:
                    f.call_refs.discard(where)
            i += 1

    def scan_code(self, root: Path) -> None:
        for path in sorted((root / "src").rglob("*.cs")):
            lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
            blocks: list[list[tuple[int, str]]] = []
            cur: list[tuple[int, str]] = []
            for no, line in enumerate(lines, 1):
                s = line.strip()
                is_comment = s.startswith("//") or s.startswith("*") or s.startswith("/*") or "//" in s
                if is_comment:
                    cur.append((no, line))
                else:
                    if cur:
                        blocks.append(cur)
                    cur = []
            if cur:
                blocks.append(cur)
            rel = path.relative_to(root)
            for block in blocks:
                text = "\n".join(l for _, l in block)
                markers = [(m.start(), m.group(1)) for m in re.finditer(r"\[(ORIGINAL|APPROX)\]", text)]
                for pos, a, b, is_call in self.addresses(text, False):
                    prev = [k for p, k in markers if p <= pos]
                    nxt = [k for p, k in markers if p > pos]
                    kind = prev[-1] if prev else (nxt[0] if nxt else None)
                    status = {"ORIGINAL": "umgesetzt", "APPROX": "genähert", None: "dokumentiert"}[kind]
                    line_no = block[0][0] + text[:pos].count("\n")
                    self.mark(a, b, status, f"{rel}:{line_no}", is_call)

    def scan_docs(self, root: Path) -> None:
        for path in sorted((root / "docs").rglob("*.md")):
            rel = path.relative_to(root)
            parts = rel.parts
            if len(parts) > 1 and parts[1] in ("archiv", "reviews"):
                continue
            if rel.as_posix() == "docs/re-abdeckung.md":
                continue
            content = path.read_text(encoding="utf-8", errors="replace")
            self.file_keywords = EXE_KEYWORDS if rel.as_posix() in OTHER_FILE_DOCS else FILE_KEYWORDS
            file_level = bool(re.search(r"Dateioffset|alle Adressen sind \*\*Dateioffsets", "\n".join(content.splitlines()[:15])))
            # paragraphs (table rows count as their own paragraph)
            lines = content.splitlines()
            para: list[tuple[int, str]] = []

            def flush():
                if not para:
                    return
                text = "\n".join(l for _, l in para)
                for pos, a, b, _ in self.addresses(text, file_level):
                    line_no = para[0][0] + text[:pos].count("\n")
                    self.mark(a, b, "dokumentiert", f"{rel}:{line_no}")
                para.clear()

            for no, line in enumerate(lines, 1):
                if not line.strip() or line.lstrip().startswith("#"):
                    flush()
                    continue
                if line.lstrip().startswith("|"):
                    flush()
                    para.append((no, line))
                    flush()
                    continue
                if re.match(r"^\s*([*-]|\d+\.)\s", line):
                    flush()
                para.append((no, line))
            flush()


# ---------------------------------------------------------------- report

def pct(part: int, whole: int) -> str:
    return f"{100 * part / whole:.0f} %" if whole else "–"


def hint(f: Function, an: Analyzer, limit: int = 3) -> str:
    bits = []
    if f.images:
        bits.append("kb\\" + ",".join(str(i) for i in sorted(f.images)[:6]))
    strs = [s for s in f.strings if not s.startswith("kb\\") and s != ".vga"][:limit]
    if strs:
        bits.append("„" + "“, „".join(s[:28].replace("|", "/") for s in strs) + "“")
    if f.start in an.loaders:
        bits.append("lädt kb\\-Bilder")
    if not bits:
        known = [an.func_at(c) for c in sorted(f.callers)]
        known = [c for c in known if c and (c.status != "unbekannt" or c.strings)]
        if known:
            c = known[0]
            what = c.strings[0][:24] if c.strings else c.status
            bits.append(f"gerufen von 0x{c.start:05X} ({what})")
    if not bits and f.globals_:
        bits.append("Globale " + ", ".join(f"0x{g:X}" for g in sorted(f.globals_)[:4]))
    return "; ".join(bits).replace("|", "/") or "–"


def region_kind(r: Region) -> str:
    if r.stub is not None:
        return "Overlay"
    return BASE_KIND.get(r.seg, ("Spiel", ""))[1]


def region_label(r: Region) -> str:
    if r.stub is not None:
        return f"Ov {r.stub:#x} ({STUB_TO_GHIDRA.get(r.stub, 0):X})"
    return f"Basis {r.seg:#x}"


GLOBALS_OF_INTEREST = [0x5A14, 0x5CF3, 0x3BD, 0xDE15]
TOP_GAPS = 20
THIN_SIZE = 2048


def theme(fs: list[Function]) -> str:
    texts = []
    for f in sorted(fs, key=lambda f: -f.size):
        for t in f.strings:
            t = t.strip()
            if len(t) >= 8 and sum(c.isalpha() for c in t) >= 5 and not t.startswith("kb\\") and t not in texts \
                    and "XOR" not in t:
                texts.append(t)
                break
        if len(texts) >= 3:
            break
    return ", ".join("„" + t[:24].replace("|", "/") + "“" for t in texts) or "–"


def render(an: Analyzer, rc: RefCollector) -> str:
    out = []
    w = out.append
    funcs = an.functions
    game = [f for f in funcs if not is_system(f.region)]
    w("<!-- BEGIN generiert: tools/reverse-engineering/funktionskarte.py – nicht von Hand ändern -->")
    w("")
    w(f"Erzeugt aus `em.exe` ({len(an.exe)} Byte): {len(funcs)} Funktionen in {len(an.regions)} Bereichen "
      f"({sum(1 for r in an.regions if r.stub is None)} Basissegmente, {sum(1 for r in an.regions if r.stub is not None)} "
      f"Overlays), {sum(f.size for f in funcs)} Byte Code; {rc.hits} Zuordnungen von Fundstellen zu Funktionen.")
    w("")
    w("### Anteile je Status (Bytes)")
    w("")
    w("„Spielcode“ = ohne die Systemsegmente (C-Laufzeit, Speicher, Grafik/Maus, Datei, Timer, Sound, Tastatur), die das Remake "
      "durch .NET/MonoGame ersetzt.")
    w("")
    w("| Status | Funktionen | Bytes | Anteil | Funktionen Spielcode | Bytes Spielcode | Anteil Spielcode |")
    w("|---|---:|---:|---:|---:|---:|---:|")
    tot, gtot = sum(f.size for f in funcs), sum(f.size for f in game)
    for st in STATUS_ORDER:
        fs = [f for f in funcs if f.status == st]
        gs = [f for f in game if f.status == st]
        w(f"| {st} | {len(fs)} | {sum(f.size for f in fs)} | {pct(sum(f.size for f in fs), tot)} | {len(gs)} | "
          f"{sum(f.size for f in gs)} | {pct(sum(f.size for f in gs), gtot)} |")
    w(f"| **Summe** | {len(funcs)} | {tot} | 100 % | {len(game)} | {gtot} | 100 % |")
    w("")
    w("### Übersicht je Bereich")
    w("")
    w("Basissegment (Segmentwert, Rolle vermutet) oder Overlay (Stub-Selektor, in Klammern der Name der alten "
      "Ghidra-Analyse). Anteile in Bytes. Texte = längste benutzte Zeichenketten der größten Funktionen (Hinweis aufs Thema).")
    w("")
    w("| Bereich | Datei | Größe | Funktionen | umgesetzt | genähert | dokumentiert | unbekannt | Rolle / Texte |")
    w("|---|---|---:|---:|---:|---:|---:|---:|---|")
    for r in an.regions:
        fs = [f for f in funcs if f.region is r]
        size = sum(f.size for f in fs)
        by = {st: sum(f.size for f in fs if f.status == st) for st in STATUS_ORDER}
        role = theme(fs) if r.stub is not None else region_kind(r)
        w(f"| {region_label(r)} | 0x{r.start:05X}–0x{r.end - 1:05X} | {r.size} | {len(fs)} | "
          + " | ".join(pct(by[st], size) for st in STATUS_ORDER) + f" | {role} |")
    w("")
    w(f"### Dünn belegte große Funktionen (> {THIN_SIZE} Byte, höchstens zwei Fundstellen)")
    w("")
    w("Der Status gilt je Funktion: schon eine Fundstelle zählt die ganze Funktion. Diese großen Funktionen sind "
      "dadurch vermutlich zu günstig eingestuft.")
    w("")
    w("| Datei | Bereich | Größe | Status | Fundstellen | Vermutung |")
    w("|---|---|---:|---|---|---|")
    for f in sorted(game, key=lambda f: -f.size):
        refs = sorted({x for v in f.refs.values() for x in v})
        if f.size > THIN_SIZE and f.status in ("umgesetzt", "dokumentiert") and len(refs) <= 2:
            w(f"| 0x{f.start:05X} | {region_label(f.region)}+0x{f.start - f.region.start:X} | {f.size} | {f.status} | "
              f"{', '.join('`' + x + '`' for x in refs)} | {hint(f, an)} |")
    w("")
    w(f"### Die {TOP_GAPS} größten Lücken (Spielcode, unbekannt oder nur genähert)")
    w("")
    w("| # | Datei | Bereich | Größe | Status | Aufrufer | Vermutung (automatisch) |")
    w("|---:|---|---|---:|---|---|---|")
    gaps = sorted((f for f in game if f.status in ("unbekannt", "genähert")), key=lambda f: -f.size)[:TOP_GAPS]
    for i, f in enumerate(gaps, 1):
        callers = ", ".join(f"0x{c:05X}" for c in sorted(f.callers)[:3]) + (" …" if len(f.callers) > 3 else "")
        w(f"| {i} | 0x{f.start:05X} | {region_label(f.region)}+0x{f.start - f.region.start:X} | {f.size} | {f.status} | "
          f"{callers or '–'} | {hint(f, an, 4)} |")
    w("")
    w("### Funktionen mit Zugriff auf offene Globale")
    w("")
    w("Direkte Zugriffe `[g]`, `[g+1]` oder `[reg±g]` im disassemblierten Code.")
    w("")
    w("| Globale (DGROUP) | Funktionen (Status) |")
    w("|---|---|")
    for g in GLOBALS_OF_INTEREST:
        fs = [f for f in funcs if g in f.globals_ or g + 1 in f.globals_]  # byte arrays indexed from 1
        w(f"| 0x{g:X} | " + ", ".join(f"0x{f.start:05X} ({f.status})" for f in fs) + " |")
    w("")
    w(f"### Alle unbekannten und nur genäherten Funktionen > {MIN_LIST_SIZE} Byte (Spielcode)")
    w("")
    w("Vermutung = automatisch abgeleitete Hinweise (benutzte Texte, auch über Zeigertabellen; Bildnummern an den "
      "kb\\-Lader; Aufrufer); **nur Vermutung**, nicht geprüft.")
    w("")
    w("| Datei | Bereich | Größe | Status | Aufrufer | Vermutung |")
    w("|---|---|---:|---|---:|---|")
    for f in sorted(game, key=lambda f: f.start):
        if f.size <= MIN_LIST_SIZE or f.status not in ("unbekannt", "genähert"):
            continue
        w(f"| 0x{f.start:05X} | {region_label(f.region)}+0x{f.start - f.region.start:X} | {f.size} | {f.status} | "
          f"{len(f.callers)} | {hint(f, an)} |")
    w("")
    w("<!-- END generiert -->")
    return "\n".join(out)


def write_csv(an: Analyzer, path: Path) -> None:
    with path.open("w", newline="", encoding="utf-8") as fh:
        wr = csv.writer(fh)
        wr.writerow(["datei", "bereich", "offset", "groesse", "status", "erkannt_durch", "stub_eintrag",
                     "aufrufer", "aufgerufene", "bilder", "texte", "globale", "fundstellen"])
        for f in an.functions:
            refs = "; ".join(f"{s}: {', '.join(v)}" for s, v in f.refs.items() if v)
            wr.writerow([f"0x{f.start:05X}", region_label(f.region), f"0x{f.start - f.region.start:X}", f.size,
                         f.status, "+".join(sorted(f.reasons)), f"0x{f.entry:X}" if f.entry is not None else "",
                         " ".join(f"0x{c:05X}" for c in sorted(f.callers)),
                         " ".join(f"0x{c:05X}" for c in sorted(f.callees)),
                         " ".join(str(i) for i in sorted(f.images)), " / ".join(f.strings[:8]),
                         " ".join(f"0x{g:X}" for g in sorted(f.globals_)), refs])


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("exe", type=Path)
    ap.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[2])
    ap.add_argument("--write", action="store_true", help="update docs/re-abdeckung.md between the markers")
    ap.add_argument("--csv", type=Path, help="write all functions as CSV (outside the repo)")
    args = ap.parse_args()
    an = Analyzer(args.exe.read_bytes())
    an.run()
    rc = RefCollector(an)
    rc.scan_code(args.repo)
    rc.scan_docs(args.repo)
    text = render(an, rc)
    if args.csv:
        write_csv(an, args.csv)
    if args.write:
        doc = args.repo / "docs" / "re-abdeckung.md"
        old = doc.read_text(encoding="utf-8")
        new = re.sub(r"<!-- BEGIN generiert.*?<!-- END generiert -->", lambda _: text, old, flags=re.S)
        if new == old and "<!-- BEGIN generiert" not in old:
            new = old.rstrip() + "\n\n" + text + "\n"
        doc.write_text(new, encoding="utf-8")
    else:
        print(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
