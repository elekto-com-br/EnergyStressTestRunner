using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ExcelDataReader;
using VoltElekto.Calendars;

namespace VoltElekto.Excel
{
    /// <summary>
    /// Classe base para leitores de Excel;
    /// </summary>
    public abstract class ExcelReaderBase
    {
        protected const int AutomaticEnd = -1;

        private readonly DataSet _excelData;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelReaderBase"/> class.
        /// </summary>
        protected ExcelReaderBase(bool fromResource, string workbookName, bool relativeToApp = false)
        {
            if (!fromResource)
            {
                if (relativeToApp)
                {
                    workbookName = GetPathFromApp(workbookName);
                }
                
                if (!File.Exists(workbookName))
                {
                    throw new FileNotFoundException("Arquivo não existe", workbookName);
                }
            }

            using var excelContent = fromResource
                ? GetWorkBookFromStream(workbookName)
                : new FileStream(workbookName, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(excelContent);
            _excelData = reader.AsDataSet();

            ExcelFile = fromResource ? "Embutido" : workbookName;
        }

        protected string ExcelFile { get; private set; }

        private IEnumerable<string> WorksheetNames
        {
            get
            {
                var all = _excelData.Tables.Count;
                for (var i = 0; i < all; ++i)
                {
                    var table = _excelData.Tables[i];
                    yield return table.TableName;
                }
            }
        }

        protected bool WorksheetExists(string name)
        {
            return WorksheetNames.Any(s => s == name);
        }

        /// <summary>
        /// Gets or sets a value indicating whether touse vba compatible references. That is: The first row and first column are 1, and not 0.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if using vba compatible references; otherwise, <c>false</c>.
        /// </value>
        protected bool UseVbaCompatibleReferences { get; set; } = true;

        private static string GetPathFromApp(string filePath)
        {
            var binDir = AppDomain.CurrentDomain.BaseDirectory;
            var testFile = Path.Combine(binDir, filePath);
            return testFile;
        }

        private void AdjustCellReference(ref int i)
        {
            if (i == AutomaticEnd)
            {
                return;
            }
            if (UseVbaCompatibleReferences)
            {
                i--;
            }
        }

        /// <summary>
        /// Ler um vetor de objetos de uma linha da planilha.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="row">linha</param>
        /// <param name="startColumn">inicio da coluna</param>
        /// <param name="endColumn">final da coluna</param>
        /// <returns>valores na coluna</returns>
        private object[] ReadLine(string worksheetName, int row, int startColumn, int endColumn)
        {
            var objectMatrix = ReadMatrix(worksheetName, row, row, startColumn, endColumn);

            var columnCount = endColumn - startColumn + 1;
            var objectArray = new object[columnCount];
            for (var i = 0; i < columnCount; i++)
            {
                objectArray[i] = objectMatrix[0, i];
            }

            return objectArray;
        }

        /// <summary>
        /// Ler um vetor de objetos de uma coluna da planilha.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="startRow">linha inicial</param>
        /// <param name="endRow">linha final</param>
        /// <param name="column">índice da coluna</param>
        /// <returns>valores na coluna</returns>
        private object[] ReadColumn(string worksheetName, int startRow, int endRow, int column)
        {
            var objectMatrix = ReadMatrix(worksheetName, startRow, endRow, column, column);

            var rowCount = objectMatrix.GetUpperBound(0) + 1;
            var objectArray = new object[rowCount];
            for (var i = 0; i < rowCount; i++)
            {
                objectArray[i] = objectMatrix[i, 0];
            }

            return objectArray;
        }

        

        /// <summary>
        /// Ler uma matriz de objetos de uma planilha.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="startRow">linha inicial</param>
        /// <param name="endRow">linha final</param>
        /// <param name="startColumn">coluna inicial</param>
        /// <param name="endColumn">coluna final</param>
        /// <returns>matriz</returns>
        private object[,] ReadMatrix(string worksheetName, int startRow, int endRow, int startColumn, int endColumn)
        {
            AdjustCellReference(ref startRow);
            AdjustCellReference(ref endRow);
            AdjustCellReference(ref startColumn);
            AdjustCellReference(ref endColumn);

            if (_excelData == null)
            {
                throw new Exception("Excel file not open");
            }

            if (endRow == AutomaticEnd)
            {
                endRow = FindEndRow(worksheetName, startRow, startColumn);
            }
            if (endColumn == AutomaticEnd)
            {
                endColumn = FindEndColumn(worksheetName, startColumn, startRow);
            }


            var rowCount = endRow - startRow + 1;
            var columnCount = endColumn - startColumn + 1;
            var objectMatrix = new object[rowCount, columnCount];

            for (var i = 0; i < rowCount; i++)
            {
                var rowExists = !((i + startRow) >= _excelData.Tables[worksheetName].Rows.Count);
                for (var j = 0; j < columnCount; j++)
                {
                    var positionExists = rowExists;
                    if (positionExists)
                    {
                        if ((j + startColumn) >= _excelData.Tables[worksheetName].Columns.Count)
                        {
                            positionExists = false;
                        }
                    }

                    if (positionExists)
                    {
                        var obj =
                            _excelData.Tables[worksheetName].Rows[i + startRow].ItemArray[
                                j + startColumn];
                        objectMatrix[i, j] = obj;
                    }
                    else
                    {
                        objectMatrix[i, j] = null;
                    }
                }
            }

            return objectMatrix;
        }

        private int FindEndRow(string worksheetName, int startRow, int column)
        {
            var rows = _excelData.Tables[worksheetName].Rows;

            var endRow = startRow + 1;
            while ((endRow < rows.Count) && !rows[endRow].IsNull(column))
            {
                endRow++;
            }
            return --endRow;
        }

        private int FindEndColumn(string worksheetName, int startColumn, int row)
        {
            var currentRow = _excelData.Tables[worksheetName].Rows[row];
            var endColumn = startColumn + 1;
            while (endColumn < currentRow.ItemArray.Length && !currentRow.IsNull(endColumn))
            {
                endColumn++;
            }
            return --endColumn;
        }

        /// <summary>
        /// Ler um vetor de datas de uma coluna da planilha.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="column">índice da coluna</param>
        /// <param name="startRow">linha inicial</param>
        /// <param name="endRow">linha final</param>
        /// <returns>valores na coluna</returns>
        protected DateTime[] ReadDateTimeColumn(string worksheetName, int startRow, int endRow, int column)
        {
            var objectList = ReadColumn(worksheetName, startRow, endRow, column);
            var dateArray = new List<DateTime>(objectList.Length);
            foreach (var t in objectList)
            {
                if (t is string)
                {
                    var s = t as string;
                    if (string.IsNullOrEmpty(s))
                    {
                        break;
                    }
                }

                var value = t is DateTime date ? date : DateTime.FromOADate(Convert.ToDouble(t));
                dateArray.Add(value);
            }

            return dateArray.ToArray();
        }

        /// <summary>
        /// Ler um double de uma célula.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="column">índice da coluna</param>
        /// <param name="row">linha</param>
        /// <returns>valor na célula</returns>
        protected double ReadDouble(string worksheetName, int row, int column)
        {
            return ReadDoubleColumn(worksheetName, row, row, column)[0];
        }

        /// <summary>
        /// Ler um vetor de doubles de uma coluna da planilha.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="column">índice da coluna</param>
        /// <param name="startRow">linha inicial</param>
        /// <param name="endRow">linha final</param>
        /// <returns>valores na coluna</returns>
        protected double[] ReadDoubleColumn(string worksheetName, int startRow, int endRow, int column)
        {
            var objectList = ReadColumn(worksheetName, startRow, endRow, column);
            var doubleArray = new List<double>(objectList.Length);
            foreach (var t in objectList)
            {
                if (t is string)
                {
                    var s = t as string;
                    if (string.IsNullOrEmpty(s))
                    {
                        break;
                    }
                }

                var value = Convert.ToDouble(t, CultureInfo.InvariantCulture);
                doubleArray.Add(value);
            }

            return doubleArray.ToArray();
        }

        /// <summary>
        /// Reads the Double matrix.
        /// </summary>
        /// <param name="worksheetName">name of the worksheet</param>
        /// <param name="startRow">start row</param>
        /// <param name="endRow">end row</param>
        /// <param name="startColumn">start column</param>
        /// <param name="endColumn">end column</param>
        /// <returns>double matrix</returns>
        protected double[,] ReadDoubleMatrix(string worksheetName, int startRow, int endRow, int startColumn,
                                             int endColumn)
        {
            var objMatrix = ReadMatrix(worksheetName, startRow, endRow, startColumn, endColumn);
            var matrix = new double[objMatrix.GetLength(0), objMatrix.GetLength(1)];
            for (var i = 0; i < objMatrix.GetLength(0); i++)
            {
                for (var j = 0; j < objMatrix.GetLength(1); j++)
                {
                    matrix[i, j] = Convert.ToDouble(objMatrix[i, j], CultureInfo.InvariantCulture);
                }
            }

            return matrix;
        }

        /// <summary>
        /// Reads the string.
        /// </summary>
        /// <param name="worksheetName">Name of the worksheet.</param>
        /// <param name="row">The row.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        protected string ReadString(string worksheetName, int row, int column)
        {
            return ReadStringColumn(worksheetName, row, row, column)[0];
        }

        /// <summary>
        /// Reads the string column.
        /// </summary>
        /// <param name="worksheetName">Name of the worksheet.</param>
        /// <param name="startRow">The start row.</param>
        /// <param name="endRow">The end row.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        protected string[] ReadStringColumn(string worksheetName, int startRow, int endRow, int column)
        {
            var objectList = ReadColumn(worksheetName, startRow, endRow, column);
            var array = new string[objectList.Length];
            for (var i = 0; i < objectList.Length; i++)
            {
                var value = (objectList[i] == null) ? string.Empty : objectList[i].ToString();
                array[i] = value;
            }

            return array;
        }

        /// <summary>
        /// Reads the string line.
        /// </summary>
        /// <param name="worksheetName">Name of the worksheet.</param>
        /// <param name="row">The row.</param>
        /// <param name="startColumn">The starting column.</param>
        /// <param name="endColumn">The ending column.</param>
        /// <returns></returns>
        protected string[] ReadStringLine(string worksheetName, int row, int startColumn, int endColumn)
        {
            var objectList = ReadLine(worksheetName, row, startColumn, endColumn);
            var array = new string[objectList.Length];
            for (var i = 0; i < objectList.Length; i++)
            {
                var value = (objectList[i] == null) ? string.Empty : objectList[i].ToString();
                array[i] = value;
            }

            return array;
        }

        /// <summary>
        /// Reads the string matrix.
        /// </summary>
        /// <param name="worksheetName">name of the worksheet</param>
        /// <param name="startRow">start row</param>
        /// <param name="endRow">end row</param>
        /// <param name="startColumn">start column</param>
        /// <param name="endColumn">end column</param>
        /// <returns>string matrix</returns>
        protected string[,] ReadStringMatrix(string worksheetName, int startRow, int endRow, int startColumn,
                                             int endColumn)
        {
            var objMatrix = ReadMatrix(worksheetName, startRow, endRow, startColumn, endColumn);
            var matrix = new string[objMatrix.GetLength(0), objMatrix.GetLength(1)];
            for (var i = 0; i < objMatrix.GetLength(0); i++)
            {
                for (var j = 0; j < objMatrix.GetLength(1); j++)
                {
                    matrix[i, j] = (objMatrix[i, j] == null) ? string.Empty : objMatrix[i, j].ToString();
                }
            }

            return matrix;
        }


        /// <summary>
        /// Ler um int de uma célula.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="column">índice da coluna</param>
        /// <param name="row">linha</param>
        /// <returns>valor na célula</returns>
        protected int ReadInt(string worksheetName, int row, int column)
        {
            return ReadIntColumn(worksheetName, row, row, column)[0];
        }

        /// <summary>
        /// Ler um vetor de int de uma coluna da planilha.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="column">índice da coluna</param>
        /// <param name="startRow">linha inicial</param>
        /// <param name="endRow">linha final</param>
        /// <returns>valores na coluna</returns>
        protected int[] ReadIntColumn(string worksheetName, int startRow, int endRow, int column)
        {
            var objectList = ReadColumn(worksheetName, startRow, endRow, column);
            var intArray = new int[objectList.Length];
            for (var i = 0; i < objectList.Length; i++)
            {
                var value = Convert.ToInt32(objectList[i]);
                intArray[i] = value;
            }

            return intArray;
        }

        /// <summary>
        /// Ler um vetor de int de uma coluna da planilha.
        /// </summary>
        /// <param name="worksheetName">nome da planilha</param>
        /// <param name="column">índice da coluna</param>
        /// <param name="startRow">linha inicial</param>
        /// <param name="endRow">linha final</param>
        /// <returns>valores na coluna</returns>
        protected int?[] ReadNullableIntColumn(string worksheetName, int startRow, int endRow, int column)
        {
            var objectList = ReadColumn(worksheetName, startRow, endRow, column);
            var resultList = new List<int?>(objectList.Length);
            foreach (var t in objectList)
            {
                int? value = null;
                
                if (t is string s)
                {
                    if (string.IsNullOrEmpty(s))
                    {
                        value = null;
                    }
                    else
                    {
                        value = Convert.ToInt32(t);
                        
                    }
                }
                else if (t is int i)
                {
                    value = i;
                }
                
                resultList.Add(value);
            }

            return resultList.ToArray();
        }

        /// <summary>
        /// Reads the GUID column.
        /// </summary>
        /// <param name="worksheetName">Name of the worksheet.</param>
        /// <param name="startRow">The start row.</param>
        /// <param name="endRow">The end row.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        protected Guid[] ReadGuidColumn(string worksheetName, int startRow, int endRow, int column)
        {
            var list = ReadStringColumn(worksheetName, startRow, endRow, column);
            var array = new Guid[list.Length];
            for (var i = 0; i < list.Length; i++)
            {
                array[i] = new Guid(list[i]);
            }

            return array;
        }

        /// <summary>
        /// Reads the GUID.
        /// </summary>
        /// <param name="worksheetName">Name of the worksheet.</param>
        /// <param name="row">The row.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        protected Guid ReadGuid(string worksheetName, int row, int column)
        {
            return new Guid(ReadString(worksheetName, row, column));
        }


        private static T Cast<T>(dynamic o)
        {
            var actualType = Nullable.GetUnderlyingType(typeof(T));
            if (actualType != null)
            {
                if (o is DBNull) return default(T);
            }
            else
            {
                actualType = typeof(T);
            }

            if (actualType == typeof(double))
            {
                return Convert.ToDouble(o, CultureInfo.InvariantCulture);
            }
            if (actualType == typeof(int))
            {
                return Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            if (actualType == typeof(DateTime))
            {
                return o is DateTime date ? date : DateTime.FromOADate(Convert.ToDouble(o, CultureInfo.InvariantCulture));
            }

            return Convert.ChangeType(o, actualType, CultureInfo.InvariantCulture);
        }

        protected T ReadValue<T>(string worksheetName, int row, int column, string defaultString = null)
        {
            return ReadColumns<T>(worksheetName, row, row, column, defaultString)[0];
        }

        /// <summary>
        /// Reads the columns.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="worksheetName">Name of the worksheet.</param>
        /// <param name="startRow">The start row.</param>
        /// <param name="endRow">The end row.</param>
        /// <param name="column">The column.</param>
        /// <param name="defaultString">Caso definida, se o valor da célula for igual irá considerar o valor default de T</param>
        /// <returns></returns>
        protected T[] ReadColumns<T>(string worksheetName, int startRow, int endRow, int column, string defaultString = null)
        {
            const int matrixCol = 0;

            var matrix = ReadMatrix(worksheetName, startRow, endRow, column, column);
            var columns = matrix.GetLength(0);
            var list = new List<T>(columns);
            for (var i = 0; i < columns; ++i)
            {
                var t = matrix[i, matrixCol];
                if (t is string)
                {
                    var s = t as string;
                    if (string.IsNullOrEmpty(s) || s.Equals(defaultString, StringComparison.InvariantCultureIgnoreCase))
                    {
                        list.Add(default(T));
                        continue;
                    }
                }

                list.Add(Cast<T>(t));
            }

            return list.ToArray();
        }

        protected T[] ReadRow<T>(string worksheetName, int row, int startColumn, int endColumn)
        {
            const int matrixRow = 0;

            var matrix = ReadMatrix(worksheetName, row, row, startColumn, endColumn);
            var columns = matrix.GetLength(1);
            var array = new T[columns];
            for (var i = 0; i < columns; ++i)
            {
                var o = matrix[matrixRow, i];
                array[i] = Cast<T>(o);
            }

            return array;
        }


        private static Stream GetWorkBookFromStream(string targetResource)
        {
            foreach (var resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                if (resourceName.ToLowerInvariant().Contains(targetResource.ToLowerInvariant()))
                {
                    return Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                }
            }
            throw new FileNotFoundException("Recurso não localizado.", targetResource);
        }

        protected bool IsEmpty(string sheetName, int row, int col)
        {
            return string.IsNullOrEmpty(ReadString(sheetName, row, col));
        }

        protected Dictionary<string, double> ReadStringDoubleColumnToDictionary(string worksheetName, int startRow, int endRow, int stringColumn, int doubleColumn)
        {
            var s = ReadStringColumn(worksheetName, startRow, endRow, stringColumn);
            var d = ReadDoubleColumn(worksheetName, startRow, endRow, doubleColumn);

            var dic = new Dictionary<string, double>(s.Length);
            for (int i = 0, j = 0; (i < d.Length) && (j < s.Length); i++, j++)
            {
                dic.Add(s[j], d[i]);
            }

            return dic;
        }

        protected ICalendar GetCalendar(string sheetName, int startRow, int column)
        {
            return new Calendars.Calendar(ReadDateTimeColumn(sheetName, startRow, AutomaticEnd, column),
                $"{sheetName}.{startRow}.{column}");
        }
    }
}
