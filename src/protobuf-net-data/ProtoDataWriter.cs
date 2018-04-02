// Copyright (c) Richard Dingwall, Arjen Post. See LICENSE in the project root for license information.

using System;
using System.Data;
using System.IO;
using ProtoBuf.Data.Internal;

namespace ProtoBuf.Data
{
    /// <summary>
    /// Serializes an <see cref="System.Data.IDataReader"/> to a binary stream.
    /// </summary>
    public class ProtoDataWriter : IProtoDataWriter
    {
        /// <summary>
        /// Serialize an <see cref="System.Data.IDataReader"/> to a binary stream using protocol-buffers.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="dataSet">The <see cref="System.Data.DataSet"/>who's contents to serialize.</param>
        public void Serialize(Stream stream, DataSet dataSet)
        {
            this.Serialize(stream, dataSet.CreateDataReader(), new ProtoDataWriterOptions());
        }

        /// <summary>
        /// Serialize an <see cref="System.Data.IDataReader"/> to a binary stream using protocol-buffers.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="dataSet">The <see cref="System.Data.DataSet"/>who's contents to serialize.</param>
        /// <param name="options">Writer options.</param>
        public void Serialize(Stream stream, DataSet dataSet, ProtoDataWriterOptions options)
        {
            this.Serialize(stream, dataSet.CreateDataReader(), options);
        }

        /// <summary>
        /// Serialize an <see cref="System.Data.IDataReader"/> to a binary stream using protocol-buffers.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="dataTable">The <see cref="System.Data.DataTable"/>who's contents to serialize.</param>
        public void Serialize(Stream stream, DataTable dataTable)
        {
            this.Serialize(stream, dataTable.CreateDataReader(), new ProtoDataWriterOptions());
        }

        /// <summary>
        /// Serialize an <see cref="System.Data.IDataReader"/> to a binary stream using protocol-buffers.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="dataTable">The <see cref="System.Data.DataTable"/>who's contents to serialize.</param>
        /// <param name="options">Writer options.</param>
        public void Serialize(Stream stream, DataTable dataTable, ProtoDataWriterOptions options)
        {
            this.Serialize(stream, dataTable.CreateDataReader(), options);
        }

        /// <summary>
        /// Serialize an <see cref="System.Data.IDataReader"/> to a binary stream using protocol-buffers.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="reader">The <see cref="System.Data.IDataReader"/>who's contents to serialize.</param>
        public void Serialize(Stream stream, IDataReader reader)
        {
            this.Serialize(stream, reader, new ProtoDataWriterOptions());
        }

        /// <summary>
        /// Serialize an <see cref="System.Data.IDataReader"/> to a binary stream using protocol-buffers.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="reader">The <see cref="System.Data.IDataReader"/>who's contents to serialize.</param>
        /// <param name="options"><see cref="ProtoDataWriterOptions"/> specifying any custom serialization options.</param>
        public void Serialize(Stream stream, IDataReader reader, ProtoDataWriterOptions options)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            // Null options are permitted to be passed in.
            options = options ?? new ProtoDataWriterOptions();

            // For a (minor) performance improvement, Serialize() has been left
            // as a single long method with functions manually inlined.
            var resultIndex = 0;

            using (var writer = new ProtoWriter(stream, null, null))
            {
                do
                {
                    // This is the underlying protocol buffers structure we use:
                    //
                    // <1 StartGroup> each DataTable
                    // <SubItem>
                    //     <2 StartGroup> each DataColumn
                    //     <SubItem>
                    //         <1 String> Column Name
                    //         <2 Variant> Column ProtoDataType (enum casted to int)
                    //     </SubItem>
                    //     <3 StartGroup> each DataRow
                    //     <SubItem>
                    //         <(# Column Index) (corresponding type)> Field Value
                    //     </SubItem>
                    // </SubItem>
                    //
                    // NB if Field Value is a DataTable, the whole DataTable is

                    // write the table
                    ProtoWriter.WriteFieldHeader(1, WireType.StartGroup, writer);

                    SubItemToken resultToken = ProtoWriter.StartSubItem(resultIndex, writer);

                    var columns = new ProtoDataColumnFactory().GetColumns(reader, options);

                    new HeaderWriter(writer).WriteHeader(columns);

                    var rowWriter = new RowWriter(writer, columns, options);

                    // write the rows
                    while (reader.Read())
                    {
                        rowWriter.WriteRow(reader);
                    }

                    ProtoWriter.EndSubItem(resultToken, writer);

                    resultIndex++;
                }
                while (reader.NextResult());
            }
        }
    }
}