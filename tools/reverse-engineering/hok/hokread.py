"""Read the encrypted stream of an original save game (*.HOK) into DGROUP-addressed blocks.

Decryption as docs/formate/spielstand-original.md ("Verschlüsselung und Prüfsumme"): stream from file offset 0xBA with the
running key k (start = byte 0xB9), plain = ((enc - k) & 0xFF) ^ k, k += 0xD7; the checksum bytes at 0x08F09 and 0x0BB4C are
stored in clear and do not advance k. Only the blocks the scripts of this folder need are listed (file offset, DGROUP
address, length; table "Blöcke im verschlüsselten Strom"). Nothing of the save game is printed by this module.
"""
from __future__ import annotations

from pathlib import Path

CHECKSUMS = (0x08F09, 0x0BB4C)

# (file offset, DGROUP address or far block name, length)
BLOCKS = {
    "0118": (0x000E2, 0x0118, 4),
    "CCA4": (0x00305, 0xCCA4, 14),
    "CCA3": (0x00313, 0xCCA3, 1),
    "D0B9": (0x00314, 0xD0B9, 1),
    "CD28": (0x00375, 0xCD28, 48),
    "CCF8": (0x003A5, 0xCCF8, 48),
    "CFFC": (0x00456, 0xCFFC, 24),
    "CED8": (0x00472, 0xCED8, 288),
    "CDB8": (0x00592, 0xCDB8, 288),
    "CD88": (0x006B2, 0xCD88, 48),
    "CD58": (0x006E2, 0xCD58, 48),
    "A0CB": (0x00850, 0xA0CB, 1584),
    "0386": (0x00E80, 0x0386, 1),
    "D0E6": (0x00E82, 0xD0E6, 1),
    "965B": (0x00E87, 0x965B, 6 * 0x1AF),
    "far:9657": (0x018A1, None, 150 * 0x3D),
    "8257": (0x03C5F, 0x8257, 5120),
    "far:8253": (0x0505F, None, 200 * 33),
    "5CCB": (0x08F97, 0x5CCB, 36),
    "07A9": (0x0903B, 0x07A9, 8),
}


def decrypt(raw: bytes) -> tuple[bytes, bool]:
    """Plain stream (same offsets as the file, clear text head kept) and whether both checksums match."""
    out = bytearray(raw)
    k = raw[0xB9]
    s = raw[0xB9]
    ok = True
    for i in range(0xBA, len(raw)):
        if i in CHECKSUMS:
            ok &= raw[i] == s & 0xFF
            continue
        p = ((raw[i] - k) & 0xFF) ^ k
        out[i] = p
        s = (s + p) & 0xFF
        k = (k + 0xD7) & 0xFF
    return bytes(out), ok


class Save:
    def __init__(self, path: str | Path):
        self.raw = Path(path).read_bytes()
        self.plain, self.checksums_ok = decrypt(self.raw)

    def block(self, name: str) -> bytes:
        off, _, n = BLOCKS[name]
        return self.plain[off:off + n]

    def dgroup(self, addr: int, n: int = 1) -> bytes:
        """Bytes at a DGROUP address inside one of the listed blocks."""
        for off, base, size in BLOCKS.values():
            if base is not None and base <= addr and addr + n <= base + size:
                return self.plain[off + addr - base:off + addr - base + n]
        raise KeyError(f"DGROUP {addr:#x} not in a listed block")

    def b(self, addr: int) -> int:
        return self.dgroup(addr)[0]

    def manager(self, m: int, field: int, n: int = 1) -> bytes:
        """Manager record field by its DGROUP address for manager 0 (e.g. 0x97EA account, 0x966F club)."""
        return self.dgroup(field + 0x1AF * m, n)

    def club(self, slot: int, field: int, n: int = 1) -> bytes:
        off = BLOCKS["far:9657"][0] + 0x3D * slot + field
        return self.plain[off:off + n]
