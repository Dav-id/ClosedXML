using System;
using System.Diagnostics;

namespace ClosedXML.Excel
{
    /// <summary>
    /// A representation of a <c>ST_Ref</c>, i.e. an area in a sheet (no reference to the sheet).
    /// </summary>
    [DebuggerDisplay("{XLHelper.GetColumnLetterFromNumber(FirstPoint.Column)}{FirstPoint.Row}:{XLHelper.GetColumnLetterFromNumber(LastPoint.Column)}{LastPoint.Row}")]
    internal readonly struct XLSheetRange : IEquatable<XLSheetRange>
    {
        public XLSheetRange(XLSheetPoint firstPoint, XLSheetPoint lastPoint)
        {
            FirstPoint = firstPoint;
            LastPoint = lastPoint;
        }

        public XLSheetRange(Int32 rowStart, Int32 columnStart, Int32 rowEnd, Int32 columnEnd)
            : this(new XLSheetPoint(rowStart, columnStart), new XLSheetPoint(rowEnd, columnEnd))
        {
        }

        /// <summary>
        /// A range that covers whole worksheet.
        /// </summary>
        public static readonly XLSheetRange Full = new(
            new XLSheetPoint(XLHelper.MinRowNumber, XLHelper.MinColumnNumber),
            new XLSheetPoint(XLHelper.MaxRowNumber, XLHelper.MaxColumnNumber));

        /// <summary>
        /// Top-left point of the sheet range.
        /// </summary>
        public readonly XLSheetPoint FirstPoint;

        /// <summary>
        /// Bottom-right point of the sheet range.
        /// </summary>
        public readonly XLSheetPoint LastPoint;

        public int Width => LastPoint.Column - FirstPoint.Column + 1;

        public int Height => LastPoint.Row - FirstPoint.Row + 1;

        public int RightColumn => LastPoint.Column;

        public int BottomRow => LastPoint.Row;

        public override bool Equals(object obj)
        {
            return obj is XLSheetRange range && Equals(range);
        }

        public bool Equals(XLSheetRange other)
        {
            return FirstPoint.Equals(other.FirstPoint) && LastPoint.Equals(other.LastPoint);
        }

        public override int GetHashCode()
        {
            return FirstPoint.GetHashCode() ^ LastPoint.GetHashCode();
        }

        public static bool operator ==(XLSheetRange left, XLSheetRange right) => left.Equals(right);

        public static bool operator !=(XLSheetRange left, XLSheetRange right) => !(left == right);


        /// <inheritdoc cref="Parse(ReadOnlySpan{char})"/>
        public static XLSheetRange Parse(String input) => Parse(input.AsSpan());

        /// <summary>
        /// Parse point per type <c>ST_Ref</c> from
        /// <a href="https://learn.microsoft.com/en-us/openspecs/office_standards/ms-oe376/e7f22870-88a1-4c06-8e5f-d035b1179c50">2.1.1119 Part 4 Section 3.18.64, ST_Ref (Cell Range Reference)</a>
        /// </summary>
        /// <remarks>Can be one cell reference (A1) or two separated by a colon (A1:B2). First reference is always in top left corner</remarks>
        /// <param name="input">Input text</param>
        /// <exception cref="FormatException">If the input doesn't match expected grammar.</exception>
        public static XLSheetRange Parse(ReadOnlySpan<char> input)
        {
            var separatorIndex = input.IndexOf(':');
            if (separatorIndex == -1)
            {
                var sheetPoint = XLSheetPoint.Parse(input);
                return new XLSheetRange(sheetPoint, sheetPoint);
            }

            var first = XLSheetPoint.Parse(input.Slice(0, separatorIndex));
            var second = XLSheetPoint.Parse(input.Slice(separatorIndex + 1, input.Length - separatorIndex - 1));
            if (first.Column > second.Column || first.Row > second.Row)
                throw new FormatException($"First reference must have smaller column and row ('{input.ToString()}')");

            return new XLSheetRange(first, second);
        }

        /// <summary>
        /// Write the sheet range to the span. If range has only one cell, write only the cell.
        /// </summary>
        /// <param name="output">Must be at least 21 chars long.</param>
        /// <returns>Number of written characters.</returns>
        public int Format(Span<char> output)
        {
            if (FirstPoint == LastPoint)
                return FirstPoint.Format(output);

            var firstPointLen = FirstPoint.Format(output);
            output[firstPointLen] = ':';
            var lastPointLen = LastPoint.Format(output.Slice(firstPointLen + 1));
            return firstPointLen + 1 + lastPointLen;
        }

        public override String ToString()
        {
            Span<char> text = stackalloc char[21];
            var len = Format(text);
            return text.Slice(0, len).ToString();
        }

        /// <summary>
        /// Return a range that contains all cells below the current range.
        /// </summary>
        /// <exception cref="InvalidOperationException">The range touches the bottom border of the sheet.</exception>
        internal XLSheetRange BelowRange()
        {
            return BelowRange(XLHelper.MaxRowNumber);
        }

        /// <summary>
        /// Get a range below the current one <paramref name="rows"/> rows.
        /// If there isn't enough rows, use as many as possible.
        /// </summary>
        /// <exception cref="InvalidOperationException">The range touches the bottom border of the sheet.</exception>
        internal XLSheetRange BelowRange(int rows)
        {
            if (LastPoint.Row >= XLHelper.MaxRowNumber)
                throw new InvalidOperationException("No cells below.");

            rows = Math.Min(rows, XLHelper.MaxRowNumber - LastPoint.Row);
            return new XLSheetRange(
                new XLSheetPoint(LastPoint.Row + 1, FirstPoint.Column),
                new XLSheetPoint(LastPoint.Row + rows, LastPoint.Column));
        }

        /// <summary>
        /// Return a range that contains all cells to the right of the range.
        /// </summary>
        /// <exception cref="InvalidOperationException">The range touches the right border of the sheet.</exception>
        internal XLSheetRange RightRange()
        {
            if (LastPoint.Column == XLHelper.MaxColumnNumber)
                throw new InvalidOperationException("No cells to the left.");

            return new XLSheetRange(
                new XLSheetPoint(FirstPoint.Row, LastPoint.Column + 1),
                new XLSheetPoint(LastPoint.Row, XLHelper.MaxColumnNumber));
        }

        internal static XLSheetRange FromRangeAddress<T>(T address)
            where T : IXLRangeAddress
        {
            var firstPoint = XLSheetPoint.FromAddress(address.FirstAddress);
            var lastPoint = XLSheetPoint.FromAddress(address.LastAddress);
            if (firstPoint.Row > lastPoint.Row || firstPoint.Column > lastPoint.Column)
                return new XLSheetRange(lastPoint, firstPoint);

            return new XLSheetRange(firstPoint, lastPoint);
        }

        public bool Contains(XLSheetPoint point)
        {
            return
                point.Row >= FirstPoint.Row && point.Row <= LastPoint.Row &&
                point.Column >= FirstPoint.Column && point.Column <= LastPoint.Column;
        }

        /// <summary>
        /// Create a new range from this one by taking a number of rows from the bottom row up.
        /// </summary>
        /// <param name="rows">How many rows to take, must be at least one.</param>
        public XLSheetRange SliceFromBottom(int rows)
        {
            if (rows < 1)
                throw new ArgumentOutOfRangeException();

            return new XLSheetRange(new XLSheetPoint(BottomRow - rows + 1, FirstPoint.Column), LastPoint);
        }

        /// <summary>
        /// Create a new range from this one by taking a number of rows from the bottom row up.
        /// </summary>
        /// <param name="columns">How many columns to take, must be at least one.</param>
        public XLSheetRange SliceFromRight(int columns)
        {
            if (columns < 1)
                throw new ArgumentOutOfRangeException();

            return new XLSheetRange(new XLSheetPoint(FirstPoint.Row, RightColumn - columns + 1), LastPoint);
        }
    }
}
