namespace UbJsharp
{
    public interface IUbJsonDeserializer
    {
        object Deserialize (byte[] bytes);
        object Deserialize (byte[] bytes, ref int pos);
    }
}