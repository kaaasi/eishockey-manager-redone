"""Decrypted view of an original save game (*.HOK) for the scripts of track `abgleich-dosbox-22b`.

Same rule as `OriginalSaveGame.Read` (src/EishockeyManager.Formats/OriginalSaveGame.cs): plain header up to 0xBA, key byte
at 0xB9, byte = (enc - k) ^ k, k += 0xD7 per data byte; the two checksum bytes (file 0x08F09 and 0x0BB4C) are skipped without
advancing the key. `Hok.at(file_offset)` returns decrypted bytes addressed by their position in the file ("HOK 0x…").
Nothing of the save is printed by this module.
"""
from pathlib import Path

PLAIN = 0xBA
KEY_AT = 0xB9
STEP = 0xD7
CHECKSUMS = (0x08F09, 0x0BB4C)

# blocks used by the scripts: name -> (file offset, length)
BLOCKS = {
    "manager": (0x00E87, 6 * 0x1AF),
    "clubs": (0x018A1, 150 * 0x3D),
    "8257": (0x03C5F, 5120),
    "pool": (0x0505F, 200 * 33),
    "squad": (0x06A27, 6 * 0x618),
    "D0C1": (0x00747, 1),
    "D0C3": (0x00760, 8),
    "D0CB": (0x00758, 8),
    "D0D3": (0x00750, 8),
    "D0DB": (0x00748, 8),
    "026C": (0x00712, 48),
    "trainplan": (0x007C6, 112),
    "day": (0x08EE7, 4),
}


class Hok:
    def __init__(self, path):
        raw = Path(path).read_bytes()
        out = bytearray(raw[:PLAIN])
        k = raw[KEY_AT]
        for pos in range(PLAIN, len(raw)):
            if pos in CHECKSUMS:
                out.append(0)
                continue
            out.append(((raw[pos] - k) & 0xFF) ^ k)
            k = (k + STEP) & 0xFF
        self.data = bytes(out)

    def at(self, off, n=1):
        return self.data[off:off + n]

    def block(self, name):
        off, n = BLOCKS[name]
        return self.data[off:off + n]

    def squad_slot(self, record, slot):
        """Squad slot (0x34 bytes) of manager record 0..5 (DGROUP 0x5DC3 + 0x618 * record + 0x34 * slot)."""
        off = BLOCKS["squad"][0] + 0x618 * record + 0x34 * slot
        return self.data[off:off + 0x34]


def layout(repo=None):
    """Block order of the save stream, read from `OriginalSaveGame.Layout` (C# source): list of (dgroup, length, far)."""
    import re
    repo = Path(repo) if repo else Path(__file__).resolve().parents[3]
    src = (repo / "src/EishockeyManager.Formats/OriginalSaveGame.cs").read_text(encoding="utf-8")
    body = src[src.index("Layout { get; } ="):]
    body = body[:body.index("];")]
    consts = {"ManagerRecordLength": 0x1AF, "ClubRecordLength": 0x3D}
    out = []
    for m in re.finditer(r"\b(B|Far)\((0x[0-9A-Fa-f]+),\s*([^,\)]+)|\bChecksum\b", body):
        if m.group(0) == "Checksum":
            out.append((None, 1, False))
            continue
        expr = m.group(3)
        for k, v in consts.items():
            expr = expr.replace(k, str(v))
        out.append((int(m.group(2), 16), int(eval(expr, {"__builtins__": {}})), m.group(1) == "Far"))
    return out


def blocks(path, repo=None):
    """Decrypted blocks of a save: list of (dgroup, far, bytes) in stream order (checksums left out)."""
    h = Hok(path)
    pos = PLAIN
    res = []
    for dg, n, far in layout(repo):
        if dg is None:
            pos += 1
            continue
        res.append((dg, far, h.data[pos:pos + n]))
        pos += n
    assert pos == len(h.data), (pos, len(h.data))
    return res
