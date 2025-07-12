using System.Data;
using System.Text.Json;

namespace Bot.Utils;

public class JsonTypeHandler<T>: SqlMapper.TypeHandler<T>
{
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        parameter.Value = JsonSerializer.Serialize(value);
    }
    public override T? Parse(object value)
    {
        if (value is null or DBNull)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>((string)value);
    }
}
