using System.Text.Json.Serialization;

namespace NetPad.Configuration;

public class ResultsOptions : ISettingsOptions
{
    public ResultsOptions()
    {
        OpenOnRun = true;
        TextWrap = false;
        DefaultMissingValues();
    }

    [JsonInclude] public bool OpenOnRun { get; private set; }
    [JsonInclude] public bool TextWrap { get; private set; }
    [JsonInclude] public string? Font { get; private set; }
    [JsonInclude] public uint MaxSerializationDepth { get; private set; }
    [JsonInclude] public uint MaxCollectionSerializeLength { get; private set; }

    public ResultsOptions SetOpenOnRun(bool openOnRun)
    {
        OpenOnRun = openOnRun;
        return this;
    }

    public ResultsOptions SetTextWrap(bool textWrap)
    {
        TextWrap = textWrap;
        return this;
    }

    public ResultsOptions SetFont(string? font)
    {
        Font = font;
        return this;
    }

    public ResultsOptions SetMaxSerializationDepth(uint maxSerializationDepth)
    {
        if (maxSerializationDepth == 0)
        {
            maxSerializationDepth = 64;
        }
        else if (maxSerializationDepth > 1000)
        {
            maxSerializationDepth = 1000;
        }

        MaxSerializationDepth = maxSerializationDepth;
        return this;
    }

    public ResultsOptions SetMaxCollectionSerializeLengthDepth(uint maxCollectionSerializeLength)
    {
        if (maxCollectionSerializeLength == 0)
        {
            maxCollectionSerializeLength = 1000;
        }
        else if (maxCollectionSerializeLength > 10000)
        {
            maxCollectionSerializeLength = 10000;
        }

        MaxCollectionSerializeLength = maxCollectionSerializeLength;
        return this;
    }

    public void DefaultMissingValues()
    {
        if (MaxSerializationDepth == 0) MaxSerializationDepth = 64;
        else if (MaxSerializationDepth > 1000) MaxSerializationDepth = 1000;

        if (MaxCollectionSerializeLength == 0) MaxCollectionSerializeLength = 1000;
        else if (MaxCollectionSerializeLength > 10000) MaxCollectionSerializeLength = 10000;
    }
}
