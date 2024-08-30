using System;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace NScript.CommonApi;

public class ApiSetting
{
    public static bool Debug { get; set; }
}

public interface IApiHandler
{
    OutputPayload Handle(String jsonParams, Payload payload);
}

public interface ITypedApiHandler<TInput, TOutput> where TOutput : BaseResult, new()
{
    /// <summary>
    /// 如果是 csharp，在相同模式下（调用方和被调用方同属于 jit 或 aot 编译）可以直接使用本方法来调用，避免序列化和反序列化的开销
    /// </summary>
    /// <param name="input"></param>
    /// <param name="payload"></param>
    /// <returns></returns>
    TOutput Invoke(TInput? input, Payload payload);
}

public abstract class ApiHandler : IApiHandler
{
    public abstract OutputPayload Handle(String jsonParams);

    public OutputPayload Handle(String jsonParams, Payload payload)
    {
        return this.Handle(jsonParams);
    }
}

public abstract class PayloadApiHandler : IApiHandler
{
    public abstract OutputPayload Handle(String jsonParams, Payload payload);
}

public abstract class TypedApiHandler<TInput, TOutput> : ApiHandler, ITypedApiHandler<TInput, TOutput> where TOutput : BaseResult, new()
{
    protected abstract ValueTuple<JsonTypeInfo<TInput>, JsonTypeInfo<TOutput>> GetTypeInfos();

    public override OutputPayload Handle(string jsonParams)
    {
        String outputStr = string.Empty;
        String err = null;
        byte[] outputByte = null;
        var pair = GetTypeInfos();
        IntPtr pOutput = IntPtr.Zero;
        OutputPayload payload = OutputPayload.Empty;
        try
        {
            TInput? input = JsonSerializer.Deserialize<TInput>(jsonParams, pair.Item1);
            TOutput output = Handle(input);
            //outputStr = JsonSerializer.Serialize<TOutput>(output, pair.Item2);
            outputByte = JsonSerializer.SerializeToUtf8Bytes<TOutput>(output, pair.Item2);
            // 将byte[]填充到payload
            payload = OutputPayload.TransferFromBytes(outputByte);
        }
        catch (JsonException ex)
        {
            err = BaseResult.CreateErrorJsonString(Error.InvalidInput);
        }
        catch (Exception ex)
        {
            String errMsg = ApiSetting.Debug == true ? (ex.Message + Environment.NewLine + ex.StackTrace) : (ex.Message);
            err = BaseResult.CreateErrorJsonString(Error.InternalError, ex.Message + Environment.NewLine + ex.StackTrace);
        }

        if (err != null) return payload;
        else return payload;
    }

    /// <summary>
    /// 处理输入，返回输出
    /// </summary>
    /// <param name="input">输入。input 将会序列化为 json 传输到 common api。</param>
    /// <param name="payload">输入的二进制负载。对于 json 序列化代价比较大的数据，可以通过 payload 直接内存传输。</param>
    /// <returns></returns>
    protected abstract TOutput Handle(TInput? input);

    public TOutput Invoke(TInput? input, Payload payload)
    {
        return Handle(input);
    }
}

public abstract class TypedPayloadApiHandler<TInput, TOutput> : PayloadApiHandler, ITypedApiHandler<TInput, TOutput> where TOutput : BaseResult, new()
{
    protected abstract ValueTuple<JsonTypeInfo<TInput>, JsonTypeInfo<TOutput>> GetTypeInfos();

    public override OutputPayload Handle(string jsonParams, Payload payload)
    {
        String outputStr = string.Empty;
        String err = null;
        IntPtr pOutput = IntPtr.Zero;
        byte[] outputByte = null;
        OutputPayload respPayload = OutputPayload.Empty;
        var pair = GetTypeInfos();
        try
        {
            TInput? input = JsonSerializer.Deserialize<TInput>(jsonParams, pair.Item1);
            TOutput output = Handle(input, payload);

            // todo 测试 直接将 output 转为byte数组
            // outputStr = JsonSerializer.Serialize<TOutput>(output, pair.Item2);
            outputByte = JsonSerializer.SerializeToUtf8Bytes<TOutput>(output, pair.Item2);
            // 将byte数组填充到payload
            respPayload = OutputPayload.TransferFromBytes(outputByte);

        }
        catch (JsonException)
        {
            err = BaseResult.CreateErrorJsonString(Error.InvalidInput);
        }
        catch (Exception ex)
        {
            String errMsg = ApiSetting.Debug == true ? (ex.Message + Environment.NewLine + ex.StackTrace) : (ex.Message);
            err = BaseResult.CreateErrorJsonString(Error.InternalError, ex.Message + Environment.NewLine + ex.StackTrace);
        }

        if (err != null) return respPayload;
        else return respPayload;
    }

    /// <summary>
    /// 处理输入，返回输出
    /// </summary>
    /// <param name="input">输入。input 将会序列化为 json 传输到 common api。</param>
    /// <param name="payload">输入的二进制负载。对于 json 序列化代价比较大的数据，可以通过 payload 直接内存传输。</param>
    /// <returns></returns>
    protected abstract TOutput Handle(TInput? input, Payload payload);

    public TOutput Invoke(TInput? input, Payload payload)
    {
        return Handle(input, payload);
    }
}

