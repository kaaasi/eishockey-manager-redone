"""Small 16-bit real-mode x86 interpreter that runs code straight from the bytes of em.exe.

Purpose (Runde 25, Spur re-abweichungen): evidence as a replay of the original code path instead of reading single
instructions (`docs/prozess/zusammenarbeit.md` §5 point 7). The scripts next to this file prepare memory (DGROUP,
far data), pick a start address, stub everything outside the code under test and print what the original computes.

Memory model: the load image (file from HEADER) sits at linear 0, so base segments and DGROUP keep their segment values
(DGROUP = 0x14FD). Overlays are copied to pseudo segments from 0x3000 on (paragraph aligned, packed); far calls are resolved with the layout of
`funktionskarte.py` (stub selectors in overlays, raw segments in base code). Unknown far/near targets can be stubbed.
No original data is written anywhere; the scripts print only addresses and computed numbers.
"""
from __future__ import annotations

import struct
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
import funktionskarte as fk  # noqa: E402

HEADER = fk.HEADER
DGROUP = fk.DGROUP_SEG
OV_SEG0 = 0x3000       # overlays packed from linear 0x30000 (all 17 together < 0x40000)
STACK_SEG = 0x9000
FAR_HEAP_SEG = 0xB000   # free linear 0xB0000 … 0xEFFFF for far data prepared by the scripts (0xA0000 = VGA)

R16 = ["ax", "cx", "dx", "bx", "sp", "bp", "si", "di"]
SREG = ["es", "cs", "ss", "ds"]
CF, PF, AF, ZF, SF, TF, IF, DF, OF = 0x1, 0x4, 0x10, 0x40, 0x80, 0x100, 0x200, 0x400, 0x800


class Stop(Exception):
    """Raised by hooks or the step limit to end a run."""


def parity(v: int) -> bool:
    return bin(v & 0xFF).count("1") % 2 == 0


class CPU:
    def __init__(self, exe: bytes):
        self.exe = exe
        self.regions = fk.read_layout(exe)
        self.an = fk.Analyzer.__new__(fk.Analyzer)
        self.an.exe = exe
        self.an.regions = self.regions
        self.an.by_seg = {r.seg: r for r in self.regions if r.seg is not None}
        self.an.by_stub = {r.stub: r for r in self.regions if r.stub is not None}
        self.an.by_stub_seg = {r.stub_seg: r for r in self.regions if r.stub is not None}
        self.mem = bytearray(0x110000)
        image = exe[HEADER:fk.OVERLAY_BASE]
        self.mem[0:len(image)] = image
        # pseudo code segments: cs -> region (base: natural segment, overlay: relocated copy)
        self.cs_region: dict[int, fk.Region] = {}
        self.region_cs: dict[str, int] = {}
        cs_next = OV_SEG0
        for r in self.regions:
            if r.seg is not None:
                self.cs_region[r.seg] = r
                self.region_cs[r.key] = r.seg
            else:
                cs = cs_next
                cs_next += (r.size + 0x2F) // 16
                self.mem[cs * 16:cs * 16 + r.size] = exe[r.start:r.end]
                self.cs_region[cs] = r
                self.region_cs[r.key] = cs
        self.r = {k: 0 for k in R16}
        self.s = {"es": DGROUP, "cs": 0, "ss": STACK_SEG, "ds": DGROUP}
        self.r["sp"] = 0xFFF0
        self.ip = 0
        self.flags = 0x0002
        self.hooks: dict[int, object] = {}      # file address -> fn(cpu) returning value for ax (or None)
        self.stub_outside = True                # stub calls that leave the allowed ranges
        self.allowed: list[tuple[int, int]] = []
        self.calls: list[tuple[int, int]] = []  # (from file address, to file address) of stubbed calls
        self.trace_calls = False
        self.steps = 0
        self.max_steps = 2_000_000
        self.stop_at: set[int] = set()
        self.int_hook = None
        self.visited: set[int] = set()
        self.record = False
        self.port_toggle = 0
        self.history: list | None = None        # last executed addresses (debugging)
        self.pre: dict[int, object] = {}         # file address -> fn(cpu) run before the instruction there

    # ------------------------------------------------------------ addresses
    def file_of(self, cs: int, ip: int) -> int:
        r = self.cs_region.get(cs)
        if r is None:
            return -1
        base = r.start if r.seg is None else HEADER + r.seg * 16
        return base + ip

    def at(self, file_addr: int) -> tuple[int, int]:
        for r in self.regions:
            if r.start <= file_addr < r.end + 16:
                cs = self.region_cs[r.key]
                base = r.start if r.seg is None else HEADER + r.seg * 16
                return cs, file_addr - base
        raise ValueError(f"no code region at {file_addr:#x}")

    def here(self) -> int:
        return self.file_of(self.s["cs"], self.ip)

    def region_here(self) -> fk.Region:
        return self.cs_region[self.s["cs"]]

    # ------------------------------------------------------------ memory
    def lin(self, seg: int, off: int) -> int:
        return ((seg << 4) + (off & 0xFFFF)) & 0xFFFFF

    def rb(self, seg, off):
        return self.mem[self.lin(seg, off)]

    def rw(self, seg, off):
        return self.rb(seg, off) | (self.rb(seg, off + 1) << 8)

    def wb(self, seg, off, v):
        self.mem[self.lin(seg, off)] = v & 0xFF

    def ww(self, seg, off, v):
        self.wb(seg, off, v)
        self.wb(seg, off + 1, v >> 8)

    # DGROUP helpers for the scripts
    def db(self, off):
        return self.rb(DGROUP, off)

    def dw(self, off):
        return self.rw(DGROUP, off)

    def sdb(self, off, v):
        self.wb(DGROUP, off, v)

    def sdw(self, off, v):
        self.ww(DGROUP, off, v)

    def cstr(self, seg, off, limit=200) -> str:
        out = bytearray()
        for i in range(limit):
            c = self.rb(seg, off + i)
            if c == 0:
                break
            out.append(c)
        return out.decode("cp437", "replace")

    # ------------------------------------------------------------ stack
    def push(self, v):
        self.r["sp"] = (self.r["sp"] - 2) & 0xFFFF
        self.ww(self.s["ss"], self.r["sp"], v)

    def pop(self):
        v = self.rw(self.s["ss"], self.r["sp"])
        self.r["sp"] = (self.r["sp"] + 2) & 0xFFFF
        return v

    def arg(self, i: int, far: bool = True) -> int:
        """Word argument i of a cdecl function at its entry (before push bp)."""
        return self.rw(self.s["ss"], self.r["sp"] + (4 if far else 2) + 2 * i)

    # ------------------------------------------------------------ registers
    def g8(self, i):
        v = self.r[R16[i & 3]]
        return (v >> 8) & 0xFF if i & 4 else v & 0xFF

    def s8(self, i, v):
        k = R16[i & 3]
        if i & 4:
            self.r[k] = (self.r[k] & 0x00FF) | ((v & 0xFF) << 8)
        else:
            self.r[k] = (self.r[k] & 0xFF00) | (v & 0xFF)

    # ------------------------------------------------------------ flags
    def setf(self, f, on):
        self.flags = (self.flags | f) if on else (self.flags & ~f)

    def getf(self, f):
        return bool(self.flags & f)

    def szp(self, v, w):
        m = 0xFFFF if w else 0xFF
        v &= m
        self.setf(ZF, v == 0)
        self.setf(SF, bool(v & (0x8000 if w else 0x80)))
        self.setf(PF, parity(v))

    def alu(self, op, a, b, w):
        m = 0xFFFF if w else 0xFF
        sign = 0x8000 if w else 0x80
        c = 1 if self.getf(CF) else 0
        if op in (0, 2):  # add, adc
            cin = c if op == 2 else 0
            r = a + b + cin
            self.setf(CF, r > m)
            self.setf(OF, bool(~(a ^ b) & (a ^ r) & sign))
            self.setf(AF, bool((a ^ b ^ r) & 0x10))
        elif op in (3, 5, 7):  # sbb, sub, cmp
            cin = c if op == 3 else 0
            r = a - b - cin
            self.setf(CF, r < 0)
            self.setf(OF, bool((a ^ b) & (a ^ r) & sign))
            self.setf(AF, bool((a ^ b ^ r) & 0x10))
        else:
            r = {1: a | b, 4: a & b, 6: a ^ b}[op]
            self.setf(CF, False)
            self.setf(OF, False)
        r &= m
        self.szp(r, w)
        return r

    def cond(self, c):
        f = self.getf
        t = [f(OF), f(CF), f(ZF), f(CF) or f(ZF), f(SF), f(PF), f(SF) != f(OF), f(ZF) or (f(SF) != f(OF))][c >> 1]
        return (not t) if c & 1 else t

    # ------------------------------------------------------------ decoding
    def fetch(self):
        v = self.rb(self.s["cs"], self.ip)
        self.ip = (self.ip + 1) & 0xFFFF
        return v

    def fetchw(self):
        v = self.fetch()
        return v | (self.fetch() << 8)

    def fetchs(self):
        v = self.fetch()
        return v - 0x100 if v & 0x80 else v

    def modrm(self):
        b = self.fetch()
        mod, reg, rm = b >> 6, (b >> 3) & 7, b & 7
        if mod == 3:
            return mod, reg, ("reg", rm)
        r = self.r
        base = [r["bx"] + r["si"], r["bx"] + r["di"], r["bp"] + r["si"], r["bp"] + r["di"],
                r["si"], r["di"], r["bp"], r["bx"]][rm]
        seg = "ss" if rm in (2, 3, 6) else "ds"
        if mod == 0 and rm == 6:
            base, seg = self.fetchw(), "ds"
        elif mod == 1:
            base += self.fetchs()
        elif mod == 2:
            base += self.fetchw()
        if self.seg_ovr:
            seg = self.seg_ovr
        return mod, reg, ("mem", seg, base & 0xFFFF)

    def rd(self, ea, w):
        if ea[0] == "reg":
            return self.r[R16[ea[1]]] if w else self.g8(ea[1])
        return self.rw(self.s[ea[1]], ea[2]) if w else self.rb(self.s[ea[1]], ea[2])

    def wr(self, ea, w, v):
        if ea[0] == "reg":
            if w:
                self.r[R16[ea[1]]] = v & 0xFFFF
            else:
                self.s8(ea[1], v)
        elif w:
            self.ww(self.s[ea[1]], ea[2], v)
        else:
            self.wb(self.s[ea[1]], ea[2], v)

    def greg(self, i, w):
        return self.r[R16[i]] if w else self.g8(i)

    def sreg(self, i, w, v):
        if w:
            self.r[R16[i]] = v & 0xFFFF
        else:
            self.s8(i, v)

    # ------------------------------------------------------------ control transfer
    def enter_target(self, cs, ip, far, src):
        target = self.file_of(cs, ip)
        hook = self.hooks.get(target)
        if hook is None and self.stub_outside and self.allowed and not any(lo <= target < hi for lo, hi in self.allowed):
            hook = "stub"
        if hook is None:
            self.s["cs"], self.ip = cs, ip
            return
        self.calls.append((src, target))
        if self.trace_calls:
            print(f"  call {src:#07x} -> {target:#07x} {'(stub)' if hook == 'stub' else ''}")
        res = None if hook == "stub" else hook(self)
        if res is not None:
            if isinstance(res, tuple):
                self.r["ax"], self.r["dx"] = res[0] & 0xFFFF, res[1] & 0xFFFF
            else:
                self.r["ax"] = res & 0xFFFF
        elif hook == "stub":
            self.r["ax"] = 0
        # return to caller (the return address is already on the stack)
        rip = self.pop()
        rcs = self.pop() if far else self.s["cs"]
        self.s["cs"], self.ip = rcs, rip

    def far_call(self, seg, off, src):
        region = self.region_here()
        target = self.an.resolve_far(region, seg, off)
        if target is None:
            raise RuntimeError(f"unresolved far call {seg:#x}:{off:#x} at {src:#x}")
        cs, ip = self.at(target)
        return cs, ip

    # ------------------------------------------------------------ run
    def call(self, file_addr: int, *args: int, far: bool = True) -> int:
        """Call a cdecl function at a file address with word arguments; returns ax."""
        cs, ip = self.at(file_addr)
        sp0 = self.r["sp"]
        for a in reversed(args):
            self.push(a)
        # return address: a sentinel
        if far:
            self.push(0xFFFF)
        self.push(0xFFFF)
        saved_cs = self.s["cs"]
        self.s["cs"], self.ip = (cs, ip) if far else (cs, ip)
        self._sentinel = (0xFFFF, 0xFFFF if far else cs)
        self.run(until_return=True)
        self.r["sp"] = sp0
        self.s["cs"] = saved_cs
        return self.r["ax"]

    def run(self, until_return=False):
        while True:
            if until_return and self.ip == 0xFFFF and self.s["cs"] in (0xFFFF, self._sentinel[1]):
                return
            here = self.here()
            if here in self.stop_at:
                raise Stop(f"stop at {here:#x}")
            if self.record:
                self.visited.add(here)
            if self.history is not None:
                self.history.append((here, self.r["sp"]))
                if len(self.history) > 40000:
                    del self.history[:20000]
            if here in self.pre:
                self.pre[here](self)
            self.steps += 1
            if self.steps > self.max_steps:
                raise Stop(f"step limit at {here:#x}")
            self.step()

    def step(self):
        self.seg_ovr = None
        rep = None
        start = self.here()
        while True:
            op = self.fetch()
            if op in (0x26, 0x2E, 0x36, 0x3E):
                self.seg_ovr = SREG[(op >> 3) & 3]
            elif op in (0xF2, 0xF3):
                rep = op
            elif op == 0xF0:
                pass
            else:
                break
        r = self.r
        if op < 0x40 and (op & 7) < 6:
            kind, w = op >> 3, op & 1
            if op & 7 < 4:
                _, reg, ea = self.modrm()
                if op & 2:
                    res = self.alu(kind, self.greg(reg, w), self.rd(ea, w), w)
                    if kind != 7:
                        self.sreg(reg, w, res)
                else:
                    res = self.alu(kind, self.rd(ea, w), self.greg(reg, w), w)
                    if kind != 7:
                        self.wr(ea, w, res)
            else:
                imm = self.fetchw() if w else self.fetch()
                res = self.alu(kind, r["ax"] if w else r["ax"] & 0xFF, imm, w)
                if kind != 7:
                    self.sreg(0, w, res)
            return
        if op in (0x06, 0x0E, 0x16, 0x1E):
            self.push(self.s[SREG[op >> 3]])
            return
        if op in (0x07, 0x17, 0x1F):
            self.s[SREG[op >> 3]] = self.pop()
            return
        if 0x40 <= op <= 0x4F:
            k = R16[op & 7]
            cf = self.getf(CF)
            r[k] = self.alu(0 if op < 0x48 else 5, r[k], 1, 1)
            self.setf(CF, cf)
            return
        if 0x50 <= op <= 0x57:
            v = r[R16[op & 7]]
            self.push(v if op != 0x54 else v)
            return
        if 0x58 <= op <= 0x5F:
            r[R16[op & 7]] = self.pop()
            return
        if op == 0x60:
            sp = r["sp"]
            for k in ("ax", "cx", "dx", "bx"):
                self.push(r[k])
            self.push(sp)
            for k in ("bp", "si", "di"):
                self.push(r[k])
            return
        if op == 0x61:
            for k in ("di", "si", "bp"):
                r[k] = self.pop()
            self.pop()
            for k in ("bx", "dx", "cx", "ax"):
                r[k] = self.pop()
            return
        if op == 0x68:
            self.push(self.fetchw())
            return
        if op == 0x6A:
            self.push(self.fetchs() & 0xFFFF)
            return
        if op in (0x69, 0x6B):
            _, reg, ea = self.modrm()
            a = self._s16(self.rd(ea, 1))
            b = self._s16(self.fetchw()) if op == 0x69 else self.fetchs()
            res = a * b
            r[R16[reg]] = res & 0xFFFF
            ov = not (-0x8000 <= res < 0x8000)
            self.setf(CF, ov)
            self.setf(OF, ov)
            return
        if 0x70 <= op <= 0x7F:
            d = self.fetchs()
            if self.cond(op & 0xF):
                self.ip = (self.ip + d) & 0xFFFF
            return
        if 0x80 <= op <= 0x83:
            w = op & 1
            _, kind, ea = self.modrm()
            if op == 0x81:
                imm = self.fetchw()
            elif op == 0x83:
                imm = self.fetchs() & 0xFFFF
            else:
                imm = self.fetch()
            res = self.alu(kind, self.rd(ea, w), imm, w)
            if kind != 7:
                self.wr(ea, w, res)
            return
        if op in (0x84, 0x85):
            w = op & 1
            _, reg, ea = self.modrm()
            self.alu(4, self.rd(ea, w), self.greg(reg, w), w)
            return
        if op in (0x86, 0x87):
            w = op & 1
            _, reg, ea = self.modrm()
            a, b = self.rd(ea, w), self.greg(reg, w)
            self.wr(ea, w, b)
            self.sreg(reg, w, a)
            return
        if 0x88 <= op <= 0x8B:
            w = op & 1
            _, reg, ea = self.modrm()
            if op & 2:
                self.sreg(reg, w, self.rd(ea, w))
            else:
                self.wr(ea, w, self.greg(reg, w))
            return
        if op == 0x8C:
            _, reg, ea = self.modrm()
            self.wr(ea, 1, self.s[SREG[reg & 3]])
            return
        if op == 0x8D:
            _, reg, ea = self.modrm()
            r[R16[reg]] = ea[2]
            return
        if op == 0x8E:
            _, reg, ea = self.modrm()
            self.s[SREG[reg & 3]] = self.rd(ea, 1)
            return
        if op == 0x8F:
            _, _, ea = self.modrm()
            self.wr(ea, 1, self.pop())
            return
        if op == 0x90:
            return
        if 0x91 <= op <= 0x97:
            k = R16[op & 7]
            r["ax"], r[k] = r[k], r["ax"]
            return
        if op == 0x98:
            al = r["ax"] & 0xFF
            r["ax"] = al | (0xFF00 if al & 0x80 else 0)
            return
        if op == 0x99:
            r["dx"] = 0xFFFF if r["ax"] & 0x8000 else 0
            return
        if op == 0x9A:
            off, seg = self.fetchw(), self.fetchw()
            cs, ip = self.far_call(seg, off, start)
            self.push(self.s["cs"])
            self.push(self.ip)
            self.enter_target(cs, ip, True, start)
            return
        if op == 0x9B:
            return
        if op == 0x9C:
            self.push(self.flags)
            return
        if op == 0x9D:
            self.flags = self.pop() | 2
            return
        if op == 0x9E:
            self.flags = (self.flags & 0xFF00) | ((r["ax"] >> 8) & 0xD5) | 2
            return
        if op == 0x9F:
            r["ax"] = (r["ax"] & 0xFF) | ((self.flags & 0xFF) << 8)
            return
        if 0xA0 <= op <= 0xA3:
            w = op & 1
            off = self.fetchw()
            seg = self.s[self.seg_ovr or "ds"]
            if op & 2:
                (self.ww if w else self.wb)(seg, off, r["ax"] if w else r["ax"] & 0xFF)
            else:
                self.sreg(0, w, self.rw(seg, off) if w else self.rb(seg, off))
            return
        if 0xA4 <= op <= 0xA7 or 0xAA <= op <= 0xAF:
            self.string_op(op, rep)
            return
        if op in (0xA8, 0xA9):
            w = op & 1
            imm = self.fetchw() if w else self.fetch()
            self.alu(4, r["ax"] if w else r["ax"] & 0xFF, imm, w)
            return
        if 0xB0 <= op <= 0xB7:
            self.s8(op & 7, self.fetch())
            return
        if 0xB8 <= op <= 0xBF:
            r[R16[op & 7]] = self.fetchw()
            return
        if op in (0xC0, 0xC1, 0xD0, 0xD1, 0xD2, 0xD3):
            w = op & 1
            _, kind, ea = self.modrm()
            n = self.fetch() if op < 0xC2 else (1 if op < 0xD2 else r["cx"] & 0xFF)
            self.wr(ea, w, self.shift(kind, self.rd(ea, w), n, w))
            return
        if op in (0xC2, 0xC3):
            n = self.fetchw() if op == 0xC2 else 0
            self.ip = self.pop()
            r["sp"] = (r["sp"] + n) & 0xFFFF
            return
        if op in (0xC4, 0xC5):
            _, reg, ea = self.modrm()
            seg = self.s[ea[1]]
            r[R16[reg]] = self.rw(seg, ea[2])
            self.s["es" if op == 0xC4 else "ds"] = self.rw(seg, ea[2] + 2)
            return
        if op in (0xC6, 0xC7):
            w = op & 1
            _, _, ea = self.modrm()
            self.wr(ea, w, self.fetchw() if w else self.fetch())
            return
        if op == 0xC8:
            size, level = self.fetchw(), self.fetch()
            self.push(r["bp"])
            fp = r["sp"]
            for i in range(1, level):
                r["bp"] = (r["bp"] - 2) & 0xFFFF
                self.push(self.rw(self.s["ss"], r["bp"]))
            if level:
                self.push(fp)
            r["bp"] = fp
            r["sp"] = (r["sp"] - size) & 0xFFFF
            return
        if op == 0xC9:
            r["sp"] = r["bp"]
            r["bp"] = self.pop()
            return
        if op in (0xCA, 0xCB):
            n = self.fetchw() if op == 0xCA else 0
            self.ip = self.pop()
            self.s["cs"] = self.pop()
            r["sp"] = (r["sp"] + n) & 0xFFFF
            return
        if op in (0xCC, 0xCD):
            n = 3 if op == 0xCC else self.fetch()
            if self.int_hook and self.int_hook(self, n):
                return
            raise RuntimeError(f"int {n:#x} at {start:#x}")
        if op in (0xD4, 0xD5):
            base = self.fetch()
            al, ah = r["ax"] & 0xFF, (r["ax"] >> 8) & 0xFF
            if op == 0xD4:
                ah, al = divmod(al, base)
            else:
                al, ah = (al + ah * base) & 0xFF, 0
            r["ax"] = (ah << 8) | al
            self.szp(al, 0)
            return
        if op == 0xD7:
            seg = self.s[self.seg_ovr or "ds"]
            self.s8(0, self.rb(seg, r["bx"] + (r["ax"] & 0xFF)))
            return
        if 0xD8 <= op <= 0xDF:
            raise RuntimeError(f"FPU opcode {op:#x} at {start:#x}")
        if 0xE0 <= op <= 0xE3:
            d = self.fetchs()
            if op == 0xE3:
                jump = r["cx"] == 0
            else:
                r["cx"] = (r["cx"] - 1) & 0xFFFF
                jump = r["cx"] != 0 and (op == 0xE2 or (op == 0xE1) == self.getf(ZF))
            if jump:
                self.ip = (self.ip + d) & 0xFFFF
            return
        if 0xE4 <= op <= 0xE7 or 0xEC <= op <= 0xEF:
            if op < 0xE8:
                self.fetch()
            if op in (0xE4, 0xE5, 0xEC, 0xED):
                # VGA status 0x3DA toggles so that retrace waits end; other ports read 0
                self.port_toggle ^= 0xFF
                r["ax"] = self.port_toggle if op in (0xEC, 0xED) and r["dx"] == 0x3DA else 0
            return
        if op == 0xE8:
            d = self.fetchw()
            target = (self.ip + d) & 0xFFFF
            # Borland idiom "push cs; call near" = far call of a function in the same segment (ends with retf)
            far = self.rb(self.s["cs"], (self.ip - 4) & 0xFFFF) == 0x0E
            self.push(self.ip)
            self.enter_target(self.s["cs"], target, far, start)
            return
        if op == 0xE9:
            d = self.fetchw()
            self.ip = (self.ip + d) & 0xFFFF
            return
        if op == 0xEA:
            off, seg = self.fetchw(), self.fetchw()
            cs, ip = self.far_call(seg, off, start)
            self.s["cs"], self.ip = cs, ip
            return
        if op == 0xEB:
            d = self.fetchs()
            self.ip = (self.ip + d) & 0xFFFF
            return
        if op == 0xF5:
            self.setf(CF, not self.getf(CF))
            return
        if op in (0xF6, 0xF7):
            self.grp3(op & 1)
            return
        if 0xF8 <= op <= 0xFD:
            f = [CF, CF, IF, IF, DF, DF][op - 0xF8]
            self.setf(f, op & 1)
            return
        if op in (0xFE, 0xFF):
            self.grp45(op & 1, start)
            return
        raise RuntimeError(f"opcode {op:#x} at {start:#x}")

    @staticmethod
    def _s16(v):
        return v - 0x10000 if v & 0x8000 else v

    @staticmethod
    def _s8(v):
        return v - 0x100 if v & 0x80 else v

    def shift(self, kind, v, n, w):
        bits = 16 if w else 8
        m = (1 << bits) - 1
        n &= 0x1F
        if n == 0:
            return v
        cf = self.getf(CF)
        for _ in range(n):
            top = (v >> (bits - 1)) & 1
            if kind == 0:  # rol
                v = ((v << 1) | top) & m
                cf = top
            elif kind == 1:  # ror
                low = v & 1
                v = (v >> 1) | (low << (bits - 1))
                cf = low
            elif kind == 2:  # rcl
                v, cf = ((v << 1) | cf) & m, top
            elif kind == 3:  # rcr
                low = v & 1
                v, cf = (v >> 1) | (cf << (bits - 1)), low
            elif kind in (4, 6):  # shl
                cf = top
                v = (v << 1) & m
            elif kind == 5:  # shr
                cf = v & 1
                v >>= 1
            elif kind == 7:  # sar
                cf = v & 1
                v = (v >> 1) | (top << (bits - 1))
        self.setf(CF, bool(cf))
        if kind >= 4:
            self.szp(v, w)
        self.setf(OF, bool(((v >> (bits - 1)) & 1) ^ cf) if kind in (0, 2, 4, 6) else False)
        return v

    def grp3(self, w):
        _, kind, ea = self.modrm()
        r = self.r
        v = self.rd(ea, w)
        if kind in (0, 1):
            imm = self.fetchw() if w else self.fetch()
            self.alu(4, v, imm, w)
        elif kind == 2:
            self.wr(ea, w, ~v)
        elif kind == 3:
            res = self.alu(5, 0, v, w)
            self.setf(CF, v != 0)
            self.wr(ea, w, res)
        elif kind in (4, 5):  # mul, imul
            if w:
                a = r["ax"]
                res = (self._s16(a) * self._s16(v)) if kind == 5 else a * v
                r["ax"], r["dx"] = res & 0xFFFF, (res >> 16) & 0xFFFF
                ov = (not -0x8000 <= res < 0x8000) if kind == 5 else r["dx"] != 0
            else:
                a = r["ax"] & 0xFF
                res = (self._s8(a) * self._s8(v)) if kind == 5 else a * v
                r["ax"] = res & 0xFFFF
                ov = (not -0x80 <= res < 0x80) if kind == 5 else (res >> 8) != 0
            self.setf(CF, ov)
            self.setf(OF, ov)
        else:  # div, idiv
            if v == 0:
                raise RuntimeError(f"division by zero at {self.here():#x}")
            if w:
                num = (r["dx"] << 16) | r["ax"]
                if kind == 7:
                    num = num - (1 << 32) if num & 0x80000000 else num
                    d = self._s16(v)
                    q = abs(num) // abs(d) * (1 if (num < 0) == (d < 0) else -1)
                    rem = num - q * d
                else:
                    q, rem = divmod(num, v)
                r["ax"], r["dx"] = q & 0xFFFF, rem & 0xFFFF
            else:
                num = r["ax"]
                if kind == 7:
                    num = self._s16(num)
                    d = self._s8(v)
                    q = abs(num) // abs(d) * (1 if (num < 0) == (d < 0) else -1)
                    rem = num - q * d
                else:
                    q, rem = divmod(num, v)
                r["ax"] = ((rem & 0xFF) << 8) | (q & 0xFF)

    def grp45(self, w, start):
        _, kind, ea = self.modrm()
        r = self.r
        if kind in (0, 1):
            cf = self.getf(CF)
            self.wr(ea, w, self.alu(0 if kind == 0 else 5, self.rd(ea, w), 1, w))
            self.setf(CF, cf)
        elif kind == 2:  # call near indirect
            target = self.rd(ea, 1)
            self.push(self.ip)
            self.enter_target(self.s["cs"], target, False, start)
        elif kind == 3:  # call far indirect
            seg = self.s[ea[1]]
            off, sg = self.rw(seg, ea[2]), self.rw(seg, ea[2] + 2)
            cs, ip = self.far_call(sg, off, start)
            self.push(self.s["cs"])
            self.push(self.ip)
            self.enter_target(cs, ip, True, start)
        elif kind == 4:
            self.ip = self.rd(ea, 1)
        elif kind == 5:
            seg = self.s[ea[1]]
            off, sg = self.rw(seg, ea[2]), self.rw(seg, ea[2] + 2)
            self.s["cs"], self.ip = self.far_call(sg, off, start)
        elif kind == 6:
            self.push(self.rd(ea, 1))
        else:
            raise RuntimeError(f"grp5/7 at {start:#x}")

    def string_op(self, op, rep):
        r = self.r
        w = op & 1
        size = 2 if w else 1
        step = -size if self.getf(DF) else size
        src = self.s[self.seg_ovr or "ds"]
        while True:
            if rep and r["cx"] == 0:
                return
            if op in (0xA4, 0xA5):
                v = self.rw(src, r["si"]) if w else self.rb(src, r["si"])
                (self.ww if w else self.wb)(self.s["es"], r["di"], v)
                r["si"] = (r["si"] + step) & 0xFFFF
                r["di"] = (r["di"] + step) & 0xFFFF
            elif op in (0xA6, 0xA7):
                a = self.rw(src, r["si"]) if w else self.rb(src, r["si"])
                b = self.rw(self.s["es"], r["di"]) if w else self.rb(self.s["es"], r["di"])
                self.alu(7, a, b, w)
                r["si"] = (r["si"] + step) & 0xFFFF
                r["di"] = (r["di"] + step) & 0xFFFF
            elif op in (0xAA, 0xAB):
                (self.ww if w else self.wb)(self.s["es"], r["di"], r["ax"] if w else r["ax"] & 0xFF)
                r["di"] = (r["di"] + step) & 0xFFFF
            elif op in (0xAC, 0xAD):
                self.sreg(0, w, self.rw(src, r["si"]) if w else self.rb(src, r["si"]))
                r["si"] = (r["si"] + step) & 0xFFFF
            elif op in (0xAE, 0xAF):
                b = self.rw(self.s["es"], r["di"]) if w else self.rb(self.s["es"], r["di"])
                self.alu(7, r["ax"] if w else r["ax"] & 0xFF, b, w)
                r["di"] = (r["di"] + step) & 0xFFFF
            if not rep:
                return
            r["cx"] = (r["cx"] - 1) & 0xFFFF
            if op in (0xA6, 0xA7, 0xAE, 0xAF):
                if (rep == 0xF3) != self.getf(ZF):
                    return


def load(path: str | None = None) -> CPU:
    p = Path(path) if path else Path.home() / "referenz/original/gamedata/em.exe"
    if not p.exists():
        alt = p.with_name("EM.EXE")
        p = alt if alt.exists() else p
    return CPU(p.read_bytes())
