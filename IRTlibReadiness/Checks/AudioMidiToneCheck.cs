using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ReadinessTool
{
    class AudioMidiToneCheck : ReadinessCheck
    {

        public AudioMidiToneCheck() : base(false, ReportMode.Info)
        {
        }

        public AudioMidiToneCheck(bool checkScopeDiag, ReportMode reportMode) : base(checkScopeDiag, reportMode)
        {
        }

        public override bool Check(CheckValue checkValue, ReportMode reportMode)
        {
            bool checkConfigurationOK = true;

            try
            {
                using (MidiOut midiOut = new MidiOut(0))
                {
                    midiOut.Send(MidiMessage.StartNote(60, 127, 1).RawData);
                    Thread.Sleep(1000);
                    midiOut.Send(MidiMessage.StopNote(60, 0, 1).RawData);
                    Thread.Sleep(1000);

                    checkResult.Result = ResultType.succeeded;
                    checkResult.ResultInfo = "Midi tone was played successfully";
                }
            }
            catch
            {
                checkResult.ResultInfo = "Error while playing midi tone";
            }

            return checkConfigurationOK;
        }

        public override string GetInfoString()
        {

            string resultString = String.Format(" - Audio: Playing Midi tone (Test: {0}) ", checkResult.Result);
            Console.WriteLine(resultString);

            return resultString;

        }
        public override CheckValue GetConfigurationDefault()
        {
            CheckValue checkValue = new CheckValue
            {
                PurposeInfo = "Checks if a midi tone can be played",
                OptionalCheck = false,
                RunThisCheck = true,
                ValidValues = new List<ValidValue>(),
                UnitInfo = "-"
            };

            return checkValue;

        }
    }
}
