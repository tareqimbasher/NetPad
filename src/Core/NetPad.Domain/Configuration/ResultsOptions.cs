using System.Text.Json.Serialization;

namespace NetPad.Configuration
{
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

        public void DefaultMissingValues()
        {
        }
    }
}
