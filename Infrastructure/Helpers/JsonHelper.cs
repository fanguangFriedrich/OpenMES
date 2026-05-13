using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Helpers
{
    public static class JsonHelper
    {
        // 使用 MakeReadOnly() 防止外部修改配置
        public static readonly JsonSerializerOptions DefaultOptions;
        public static readonly JsonSerializerOptions CamelCaseOptions;

        static JsonHelper()
        {
            DefaultOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            CamelCaseOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            };

            // .NET 8+
            DefaultOptions.MakeReadOnly();
            CamelCaseOptions.MakeReadOnly();
        }

        public static string SerializeCamelCase(object obj)
            => JsonSerializer.Serialize(obj, CamelCaseOptions);

        // ── 基础序列化 ──────────────────────────────────────────

        public static string Serialize(object obj)
            => JsonSerializer.Serialize(obj, DefaultOptions);

        // 返回 T? 明确告知调用方结果可能为 null
        public static T? Deserialize<T>(string json)
            => JsonSerializer.Deserialize<T>(json, DefaultOptions);

        // ── 安全版（不抛异常） ──────────────────────────────────

        public static bool TrySerialize(object obj, out string result)
        {
            try
            {
                result = Serialize(obj);
                return true;
            }
            catch
            {
                result = string.Empty;
                return false;
            }
        }

        public static bool TryDeserialize<T>(string json, out T? result)
        {
            try
            {
                result = Deserialize<T>(json);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        // ── 深拷贝 ──────────────────────────────────────────────

        public static T? DeepClone<T>(T obj)
        {
            var json = Serialize(obj);
            return Deserialize<T>(json);
        }

        // ── 合法性校验 ──────────────────────────────────────────

        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                using var doc = JsonDocument.Parse(json);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        // ── 异步（适合大对象写入 Stream，如 HTTP Response） ─────

        public static async Task<string> SerializeAsync<T>(T obj)
        {
            using var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, obj, DefaultOptions);
            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        public static async Task<T?> DeserializeAsync<T>(Stream stream)
            => await JsonSerializer.DeserializeAsync<T>(stream, DefaultOptions);
    }
}