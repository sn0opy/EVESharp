using System;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;
using PythonTypes.Types.Primitives;

namespace PythonTypes.Types.Database
{
    /// <summary>
    /// Helper class to work with DBRowDescriptors used by the EVE Online client.
    ///
    /// DBRowDescriptors are a Python representation of a table's header (column names and types)
    /// </summary>
    public class DBRowDescriptor
    {
        /// <summary>
        /// Column representation for the DBRowDescriptors
        /// </summary>
        public class Column
        {
            public string Name { get; }
            public FieldType Type { get; }

            public Column(string name, FieldType type)
            {
                this.Name = name;
                this.Type = type;
            }

            public Column(string name, int type)
            {
                this.Name = name;
                this.Type = (FieldType) type;
            }

            public static implicit operator PyDataType(Column column)
            {
                return new PyTuple(new PyDataType[]
                {
                    column.Name,
                    (int) column.Type
                });
            }

            public static implicit operator Column(PyDataType column)
            {
                PyTuple tuple = column as PyTuple;

                return new Column(
                    tuple[0] as PyString,
                    tuple[1] as PyInteger
                );
            }
        };

        /// <summary>
        /// Name of the PyObjects that represent a DBRowDescriptor
        /// </summary>
        private const string TYPE_NAME = "blue.DBRowDescriptor";

        public List<Column> Columns { get; }

        public DBRowDescriptor()
        {
            this.Columns = new List<Column>();
        }

        public static implicit operator PyObject(DBRowDescriptor descriptor)
        {
            PyTuple args = new PyTuple(descriptor.Columns.Count);
            int index = 0;

            foreach (Column col in descriptor.Columns)
                args[index++] = col;

            args = new PyTuple(new PyDataType[] {args});
            // build the args tuple
            return new PyObject(
                false,
                new PyTuple(new PyDataType[] { new PyToken(TYPE_NAME), args })
            );
        }

        public static implicit operator DBRowDescriptor(PyObject descriptor)
        {
            if (descriptor.Header.Count != 2)
                throw new Exception($"{TYPE_NAME} does not contain 2 elements in the header");
            if(descriptor.Header[0] is PyToken == false || descriptor.Header[0] as PyToken != TYPE_NAME)
                throw new Exception($"Expected PyObject of type {TYPE_NAME}");
            if (descriptor.Header[1] is PyTuple == false)
                throw new Exception($"{TYPE_NAME} does not contain an args tuple");

            PyTuple args = descriptor.Header[1] as PyTuple;

            DBRowDescriptor output = new DBRowDescriptor();

            foreach(PyTuple tuple in args[0] as PyTuple)
                output.Columns.Add(tuple);

            return output;
        }

        public static implicit operator DBRowDescriptor(PyDataType descriptor)
        {
            return descriptor as PyObject;
        }

        /// <summary>
        /// Creates a new DBRowDescriptor based off a specific result set from a MySqlDataReader
        /// </summary>
        /// <param name="reader">The MySqlDataReader to use when creating the DBRowDescriptor</param>
        /// <returns>Instance of a new DBRowDescriptor</returns>
        /// <exception cref="InvalidDataException">If any error was found on the creation</exception>
        public static DBRowDescriptor FromMySqlReader(MySqlDataReader reader)
        {
            DBRowDescriptor descriptor = new DBRowDescriptor();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                FieldType fieldType = Utils.GetFieldType(reader, i);

                descriptor.Columns.Add(
                    new Column(reader.GetName(i), fieldType)
                );
            }

            return descriptor;
        }
    }
}