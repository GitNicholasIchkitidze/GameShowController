using GameController.Server.Services.SoundLight;
using GameController.Shared.Models;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.MusicTheory;
using Microsoft.Extensions.Options;


namespace GameController.Server.Services
{
    public class MidiLightingService : IMidiLightingService, IDisposable
    {
        private IOutputDevice? _midiOutputDevice;
        private readonly ILogger<MidiLightingService> _logger;
        private readonly MidiSettingsModels _midiSettings;

        public event EventHandler<bool>? ConnectionStatusChanged;
        public bool IsConnected => _midiOutputDevice != null;
        public bool IsLightControlEnabled { get; set; } = true; // თუ საჭიროა, შეგიძლიათ შეცვალოთ ეს თვისება

        public MidiLightingService(ILogger<MidiLightingService> logger, IOptions<MidiSettingsModels> midiSettings)
        {
            _logger = logger;
            _midiSettings = midiSettings.Value;



            // Initialize() მეთოდის გამოძახება _midiSettings-ის ინიციალიზაციის შემდეგ
            Initialize();

            // ხელმისაწვდომი MIDI მოწყობილობების ლოგირება

        }

        private void Initialize()
        {
            try
            {
                var deviceName = _midiSettings.DeviceName;

                foreach (var device in OutputDevice.GetAll())
                {
                    _logger.LogInformation($" - {device.Name}");
                }

                _logger.LogInformation($" Midi Dev from Settings - {deviceName}");


                _midiOutputDevice = OutputDevice.GetAll().FirstOrDefault(d => d.Name.Contains("USB MIDI"));

                //_midiOutputDevice = OutputDevice.GetByName(deviceName);
                _midiOutputDevice.EventSent += OnEventSent;

                Connect();
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Successfully connected to MIDI device: {deviceName}");
                ConnectionStatusChanged?.Invoke(this, true);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error connecting to MIDI device: {ex.Message}");
                _midiOutputDevice = null;
                ConnectionStatusChanged?.Invoke(this, false);

            }
        }

        public void Connect()
        {
            if (IsConnected)
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Already connected to a MIDI device.");
                return;
            }

            try
            {
                var deviceName = _midiSettings.DeviceName;
                //_midiOutputDevice = OutputDevice.GetByName(deviceName);
                _midiOutputDevice = OutputDevice.GetAll().FirstOrDefault(d => d.Name.Contains("USB MIDI"));
                _midiOutputDevice.EventSent += OnEventSent;

                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Successfully connected to MIDI device: {deviceName}");
                ConnectionStatusChanged?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error connecting to MIDI device: {ex.Message}");
                _midiOutputDevice = null;
                ConnectionStatusChanged?.Invoke(this, false);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Not connected to a MIDI device.");
                return;
            }

            if (_midiOutputDevice == null)
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Not Existing MIDI device.");
                ConnectionStatusChanged?.Invoke(this, false); // ყოველთვის დარწმუნდით, რომ UI განახლებულია
                return;
            }

            ConnectionStatusChanged?.Invoke(this, false);
            _midiOutputDevice!.Dispose();
            _midiOutputDevice = null;
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} Successfully disconnected from MIDI device.");

        }

        // ვარიანტი 1: MIDI ნოტის ნომრით
        public void SendNoteOn(int noteNumber, int velocity)
        {

            if (!IsLightControlEnabled)
            {
                return;
            }

            if (!IsConnected)
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} MIDI device is not connected.");
                return;
            }

            // Note-ის ობიექტი შექმნა MIDI ნოტის ნომრიდან
            try
            {
                //var note = Melanchall.DryWetMidi.MusicTheory.Note.Get(new SevenBitNumber((byte)noteNumber));
                var noteOn = new NoteOnEvent((SevenBitNumber)noteNumber, (SevenBitNumber)velocity);

                _midiOutputDevice!.SendEvent(noteOn);
                //_midiOutputDevice!.SendEvent(new NoteOnEvent(note.NoteNumber, new SevenBitNumber((byte)velocity)));
                _logger.LogInformation($"MIDI device Was Sent {noteNumber}, {velocity}.");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"MIDI device Was Not Sent {ex.Message}.");

            }
        }

        public void SendNoteOff(int noteNumber)
        {
            if (!IsLightControlEnabled)
            {
                return;
            }
            if (!IsConnected)
            {
                return;
            }

            // Note-ის ობიექტი შექმნა MIDI ნოტის ნომრიდან
            var note = Melanchall.DryWetMidi.MusicTheory.Note.Get(new SevenBitNumber((byte)noteNumber));
            _midiOutputDevice!.SendEvent(new NoteOffEvent(note.NoteNumber, (SevenBitNumber)0));
        }

        // ვარიანტი 2: NoteName-ით და Octave-ით
        public void SendNoteOn(NoteName noteName, Octave octave, int velocity)
        {
            if (!IsLightControlEnabled)
            {
                return;
            }

            if (!IsConnected)
            {
                return;
            }

            // Note-ის ობიექტი შექმნა NoteName-ით და Octave-ით
            var note = Melanchall.DryWetMidi.MusicTheory.Note.Get(noteName, octave.A.Octave);
            _midiOutputDevice!.SendEvent(new NoteOnEvent(note.NoteNumber, new SevenBitNumber((byte)velocity)));
        }

        public void SendNoteOff(NoteName noteName, Octave octave)
        {
            if (!IsLightControlEnabled)
            {
                return;
            }

            if (!IsConnected)
            {
                return;
            }

            // Note-ის ობიექტი შექმნა NoteName-ით და Octave-ით
            var note = Melanchall.DryWetMidi.MusicTheory.Note.Get(noteName, octave.A.Octave);
            _midiOutputDevice!.SendEvent(new NoteOffEvent(note.NoteNumber, (SevenBitNumber)0));
        }

        private void OnEventSent(object sender, MidiEventSentEventArgs e)
        {
            _logger.LogInformation($"Sent MIDI event: {e.Event}");
        }

        public void Dispose()
        {
            if (_midiOutputDevice != null)
            {
                _midiOutputDevice.Dispose();
                _midiOutputDevice = null;
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now.ToString("yyyy-MM-dd hh.mm.ss:ffffff")} MIDI device disconnected.");
            }
        }
    }
}