namespace VeloxPdf.Fonts;

using System.Text;

/// <summary>
/// Pure-.NET binary parser for TrueType / OpenType font files.
/// Reads big-endian binary data without any external dependencies.
/// </summary>
public sealed class TrueTypeParser
{
    private readonly byte[] _data;

    public TrueTypeParser(byte[] ttfData)
    {
        _data = ttfData ?? throw new ArgumentNullException(nameof(ttfData));
    }

    // ── Table directory ─────────────────────────────────────────────────────

    private readonly record struct TableRecord(uint Tag, uint Checksum, uint Offset, uint Length);

    private Dictionary<string, TableRecord>? _tables;

    private Dictionary<string, TableRecord> GetTables()
    {
        if (_tables != null) return _tables;
        _tables = new Dictionary<string, TableRecord>(StringComparer.Ordinal);

        if (_data.Length < 12) return _tables;

        int numTables = ReadUInt16(4);
        int pos = 12; // sfVersion(4) + numTables(2) + searchRange(2) + entrySelector(2) + rangeShift(2)

        for (int i = 0; i < numTables && pos + 16 <= _data.Length; i++)
        {
            uint tag = ReadUInt32BE((uint)pos);
            uint checksum = ReadUInt32BE((uint)(pos + 4));
            uint offset = ReadUInt32BE((uint)(pos + 8));
            uint length = ReadUInt32BE((uint)(pos + 12));
            string tagStr = Encoding.ASCII.GetString(_data, pos, 4);
            _tables[tagStr] = new TableRecord(tag, checksum, offset, length);
            pos += 16;
        }
        return _tables;
    }

    private bool TryGetTable(string name, out uint offset, out uint length)
    {
        var tables = GetTables();
        if (tables.TryGetValue(name, out TableRecord rec))
        {
            offset = rec.Offset;
            length = rec.Length;
            return true;
        }
        offset = 0;
        length = 0;
        return false;
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>Extract the PostScript name (nameId=6) from the 'name' table.</summary>
    public string ParsePostScriptName()
    {
        if (!TryGetTable("name", out uint tableOffset, out _))
            return "UnknownFont";

        if (tableOffset + 6 > _data.Length) return "UnknownFont";

        ushort count = ReadUInt16((int)tableOffset + 2);
        ushort stringStorageOffset = ReadUInt16((int)tableOffset + 4);

        for (int i = 0; i < count; i++)
        {
            int recPos = (int)tableOffset + 6 + i * 12;
            if (recPos + 12 > _data.Length) break;

            ushort platformId = ReadUInt16(recPos);
            // ushort encodingId = ReadUInt16(recPos + 2); // unused here
            // ushort languageId = ReadUInt16(recPos + 4); // unused here
            ushort nameId = ReadUInt16(recPos + 6);
            ushort strLen = ReadUInt16(recPos + 8);
            ushort strOff = ReadUInt16(recPos + 10);

            if (nameId != 6) continue; // PostScript name

            int absPos = (int)tableOffset + stringStorageOffset + strOff;
            if (absPos + strLen > _data.Length) continue;

            if (platformId == 1) // Macintosh (Mac Roman = ASCII for PS names)
                return Encoding.ASCII.GetString(_data, absPos, strLen);

            // Platform 3 (Windows) or 0 (Unicode) — UTF-16 BE
            var bytes = new byte[strLen];
            Array.Copy(_data, absPos, bytes, 0, strLen);
            return Encoding.BigEndianUnicode.GetString(bytes).Replace("\0", "");
        }
        return "UnknownFont";
    }

    /// <summary>Units per em from the 'head' table.</summary>
    public float UnitsPerEm()
    {
        if (!TryGetTable("head", out uint offset, out _)) return 1000f;
        if (offset + 20 > _data.Length) return 1000f;
        ushort upm = ReadUInt16((int)offset + 18);
        return upm == 0 ? 1000f : upm;
    }

    /// <summary>Typographic ascender in 1/1000 units, from 'hhea'.</summary>
    public float Ascender()
    {
        if (!TryGetTable("hhea", out uint offset, out _)) return 800f;
        if (offset + 6 > _data.Length) return 800f;
        float upm = UnitsPerEm();
        short asc = ReadInt16((int)offset + 4);
        return asc / upm * 1000f;
    }

    /// <summary>Typographic descender in 1/1000 units (negative), from 'hhea'.</summary>
    public float Descender()
    {
        if (!TryGetTable("hhea", out uint offset, out _)) return -200f;
        if (offset + 8 > _data.Length) return -200f;
        float upm = UnitsPerEm();
        short desc = ReadInt16((int)offset + 6);
        return desc / upm * 1000f;
    }

    /// <summary>Cap height in 1/1000 units, from 'OS/2' v2+. Falls back to 700.</summary>
    public float CapHeight()
    {
        if (!TryGetTable("OS/2", out uint offset, out _)) return 700f;
        if (offset + 2 > _data.Length) return 700f;
        ushort version = ReadUInt16((int)offset);
        if (version < 2 || offset + 90 > _data.Length) return 700f;
        float upm = UnitsPerEm();
        short capH = ReadInt16((int)offset + 88);
        return capH == 0 ? 700f : capH / upm * 1000f;
    }

    /// <summary>Italic angle (degrees) from 'post' table.</summary>
    public float ItalicAngle()
    {
        if (!TryGetTable("post", out uint offset, out _)) return 0f;
        if (offset + 8 > _data.Length) return 0f;
        // Fixed 16.16 at bytes 4-7
        int hi = (short)ReadUInt16((int)offset + 4);
        int lo = ReadUInt16((int)offset + 6);
        return hi + lo / 65536f;
    }

    /// <summary>Whether the font is monospaced, from 'post' table.</summary>
    public bool IsFixedPitch()
    {
        if (!TryGetTable("post", out uint offset, out _)) return false;
        if (offset + 16 > _data.Length) return false;
        return ReadUInt32BE((uint)(offset + 12)) != 0;
    }

    /// <summary>Get advance widths in 1/1000 units for char codes [firstChar, lastChar].</summary>
    public float[] GetGlyphWidths(int firstChar, int lastChar)
    {
        int count = lastChar - firstChar + 1;
        var widths = new float[count];
        float upm = UnitsPerEm();

        int[] glyphIds = GetGlyphIds(firstChar, lastChar);
        if (!TryGetTable("hmtx", out uint hmtxOffset, out _)) return widths;
        if (!TryGetTable("hhea", out uint hheaOffset, out _)) return widths;
        if (hheaOffset + 36 > _data.Length) return widths;

        int numHMetrics = ReadUInt16((int)hheaOffset + 34);

        for (int i = 0; i < count; i++)
        {
            int gid = glyphIds[i];
            int advanceWidth;
            if (gid > 0 && gid < numHMetrics)
            {
                int pos = (int)hmtxOffset + gid * 4;
                advanceWidth = pos + 1 < _data.Length ? ReadUInt16(pos) : 0;
            }
            else if (numHMetrics > 0)
            {
                // Last entry applies to all glyphs >= numHMetrics
                int pos = (int)hmtxOffset + (numHMetrics - 1) * 4;
                advanceWidth = pos + 1 < _data.Length ? ReadUInt16(pos) : 0;
            }
            else
            {
                advanceWidth = 0;
            }
            widths[i] = upm > 0 ? advanceWidth / upm * 1000f : 500f;
        }
        return widths;
    }

    /// <summary>Map character codes to glyph IDs via cmap format 4.</summary>
    public int[] GetGlyphIds(int firstChar, int lastChar)
    {
        int count = lastChar - firstChar + 1;
        var ids = new int[count];

        if (!TryGetTable("cmap", out uint cmapOffset, out _)) return ids;
        if (cmapOffset + 4 > _data.Length) return ids;

        ushort numSubtables = ReadUInt16((int)cmapOffset + 2);
        uint format4Offset = 0;

        for (int i = 0; i < numSubtables; i++)
        {
            int entryPos = (int)cmapOffset + 4 + i * 8;
            if (entryPos + 8 > _data.Length) break;

            ushort platformId = ReadUInt16(entryPos);
            // encodingId at entryPos+2 (not needed for format selection here)
            uint relativeOffset = ReadUInt32BE((uint)(entryPos + 4));
            uint absOffset = cmapOffset + relativeOffset;

            if (absOffset + 2 > _data.Length) continue;
            ushort format = ReadUInt16((int)absOffset);

            if (format == 4 && (platformId == 0 || platformId == 3))
            {
                format4Offset = absOffset;
                break;
            }
        }

        if (format4Offset == 0) return ids;
        return ParseFormat4(format4Offset, firstChar, lastChar, ids);
    }

    // ── Format 4 cmap parser ────────────────────────────────────────────────

    private int[] ParseFormat4(uint baseOffset, int firstChar, int lastChar, int[] ids)
    {
        if (baseOffset + 14 > _data.Length) return ids;

        ushort segCountX2 = ReadUInt16((int)baseOffset + 6);
        int segCount = segCountX2 / 2;
        if (segCount == 0) return ids;

        int endCodesBase      = (int)baseOffset + 14;
        int startCodesBase    = endCodesBase + 2 + segCount * 2;      // skip reservedPad
        int idDeltasBase      = startCodesBase + segCount * 2;
        int idRangeOffsetsBase = idDeltasBase + segCount * 2;
        int glyphIdArrayBase  = idRangeOffsetsBase + segCount * 2;

        if (idRangeOffsetsBase + segCount * 2 > _data.Length) return ids;

        for (int c = firstChar; c <= lastChar; c++)
        {
            int idx = c - firstChar;

            // Find segment whose endCode >= c
            int seg = -1;
            for (int s = 0; s < segCount; s++)
            {
                int ecPos = endCodesBase + s * 2;
                if (ecPos + 1 >= _data.Length) break;
                ushort endCode = ReadUInt16(ecPos);
                if (c <= endCode) { seg = s; break; }
            }
            if (seg < 0) continue;

            int scPos = startCodesBase + seg * 2;
            if (scPos + 1 >= _data.Length) continue;
            ushort startCode = ReadUInt16(scPos);
            if (c < startCode) continue;

            int deltaPos = idDeltasBase + seg * 2;
            if (deltaPos + 1 >= _data.Length) continue;
            short idDelta = ReadInt16(deltaPos);

            int iroPos = idRangeOffsetsBase + seg * 2;
            if (iroPos + 1 >= _data.Length) continue;
            ushort idRangeOffset = ReadUInt16(iroPos);

            int glyphId;
            if (idRangeOffset == 0)
            {
                glyphId = (c + idDelta) & 0xFFFF;
            }
            else
            {
                // glyphIdArray index = iroPos + idRangeOffset + (c - startCode) * 2
                int glyphIdPos = iroPos + idRangeOffset + (c - startCode) * 2;
                if (glyphIdPos + 1 >= _data.Length) continue;
                glyphId = ReadUInt16(glyphIdPos);
                if (glyphId != 0)
                    glyphId = (glyphId + idDelta) & 0xFFFF;
            }
            ids[idx] = glyphId;
        }
        return ids;
    }

    // ── Binary reading helpers (big-endian) ─────────────────────────────────

    private ushort ReadUInt16(int pos)
    {
        if (pos + 1 >= _data.Length) return 0;
        return (ushort)((_data[pos] << 8) | _data[pos + 1]);
    }

    private short ReadInt16(int pos) => (short)ReadUInt16(pos);

    private uint ReadUInt32BE(uint pos)
    {
        if (pos + 3 >= _data.Length) return 0u;
        return (uint)((_data[pos] << 24) | (_data[pos + 1] << 16) | (_data[pos + 2] << 8) | _data[pos + 3]);
    }
}
