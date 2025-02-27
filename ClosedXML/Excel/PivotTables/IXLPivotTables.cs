#nullable disable

using System;
using System.Collections.Generic;

namespace ClosedXML.Excel
{
    public interface IXLPivotTables : IEnumerable<IXLPivotTable>
    {
        /// <summary>
        /// Add a pivot table that will use the pivot cache.
        /// </summary>
        /// <param name="name">Name of new pivot table.</param>
        /// <param name="targetCell">A cell where will the pivot table be have it's left top corner.</param>
        /// <param name="pivotCache">Pivot cache to use for the pivot table.</param>
        /// <returns>Added pivot table.</returns>
        /// <exception cref="ArgumentException">There already is a pivot table with the same name.</exception>
        IXLPivotTable Add(String name, IXLCell targetCell, IXLPivotCache pivotCache);

        IXLPivotTable Add(String name, IXLCell targetCell, IXLRange range);

        IXLPivotTable Add(String name, IXLCell targetCell, IXLTable table);

        Boolean Contains(String name);

        void Delete(String name);

        void DeleteAll();

        /// <summary>
        /// Get pivot table with the specified name (case insensitive).
        /// </summary>
        /// <param name="name">Name of a pivot table to return.</param>
        /// <exception cref="KeyNotFoundException">No such pivot table found.</exception>
        IXLPivotTable PivotTable(String name);
    }
}
