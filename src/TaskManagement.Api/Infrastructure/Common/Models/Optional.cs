using System.Text.Json.Serialization;
using TaskManagement.Api.Infrastructure.Common.Serialization;

namespace TaskManagement.Api.Infrastructure.Common.Models
{
    [JsonConverter(typeof(OptionalJsonConverterFactory))]
    public readonly struct Optional<T>
    {
        private readonly T? _value;

        public bool HasValue { get; }
        public T? Value => _value;

        private Optional(T? value)
        {
            _value = value;
            HasValue = true;
        }

        public static Optional<T> FromValue(T? value) => new(value);

        public static implicit operator Optional<T>(T? value) => FromValue(value);
    }
}
