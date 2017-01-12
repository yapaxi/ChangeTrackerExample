using Autofac;
using Common;
using IntegrationService.Host.Converters;
using IntegrationService.Host.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationService.Host.Writers
{
    public class DataFlow<TSource, TResult> : IDataFlow<TSource>
    {
        private readonly IConverter<TSource, TResult> _converter;
        private readonly WriteDestination _destination;
        private readonly RuntimeMappingSchema _schema;
        private readonly IWriter<TResult> _writer;

        public DataFlow(IConverter<TSource, TResult> converter, IWriter<TResult> writer, RuntimeMappingSchema schema, WriteDestination destination)
        {
            _converter = converter;
            _writer = writer;
            _destination = destination;
            _schema = schema;
        }

        public void Write(TSource rawMessage)
        {
            var convertedMessages = _converter.Convert(rawMessage, _schema);
            _writer.Write(convertedMessages, _destination);
        }
    }
}
