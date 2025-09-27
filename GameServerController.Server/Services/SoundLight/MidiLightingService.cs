using GameController.Shared.Models;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.MusicTheory;
using Microsoft.Extensions.Options;

namespace GameController.Server.Services.SoundLight
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

        // MidiLightingService.cs-ში
        private void Initialize()
        {
            try
            {
                var deviceName = _midiSettings.DeviceName;

                // დაბეჭდეთ ყველა ხელმისაწვდომი მოწყობილობა დეტალურად
                _logger.LogInformation("Available MIDI devices:");
                foreach (var device in OutputDevice.GetAll())
                {
                    _logger.LogInformation($" - '{device.Name}' (Exact match: {device.Name == deviceName})");
                }

                // ზუსტი დამთხვევის მოძიება
                _midiOutputDevice = OutputDevice.GetAll()
                    .FirstOrDefault(d => d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));

                if (_midiOutputDevice == null)
                {
                    // თუ ზუსტი არ მოიძებნა, სცადეთ ნაწილობრივი დამთხვევა
                    _midiOutputDevice = OutputDevice.GetAll()
                        .FirstOrDefault(d => d.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase));
                }

                if (_midiOutputDevice != null)
                {
                    _midiOutputDevice.EventSent += OnEventSent;
                    _logger.LogInformation($"Successfully connected to MIDI device: {_midiOutputDevice.ToString()}");
                    ConnectionStatusChanged?.Invoke(this, true);
                }
                else
                {
                    _logger.LogError($"MIDI device '{deviceName}' not found!");
                    ConnectionStatusChanged?.Invoke(this, false);
                }
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
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Already connected to a MIDI device.");
                return;
            }

            try
            {
                var deviceName = _midiSettings.DeviceName;
                //_midiOutputDevice = OutputDevice.GetByName(deviceName);
                _midiOutputDevice = OutputDevice.GetAll().FirstOrDefault(d => d.Name.Contains("USB MIDI"));
                _midiOutputDevice.EventSent += OnEventSent;

                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Successfully connected to MIDI device: {deviceName}");
                ConnectionStatusChanged?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{Environment.NewLine}{DateTime.Now} Error connecting to MIDI device: {ex.Message}");
                _midiOutputDevice = null;
                ConnectionStatusChanged?.Invoke(this, false);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Not connected to a MIDI device.");
                return;
            }

            if (_midiOutputDevice == null)
            {
                _logger.LogError($"{Environment.NewLine}{DateTime.Now} Not Existing MIDI device.");
                ConnectionStatusChanged?.Invoke(this, false); // ყოველთვის დარწმუნდით, რომ UI განახლებულია
                return;
            }

            ConnectionStatusChanged?.Invoke(this, false);
            _midiOutputDevice!.Dispose();
            _midiOutputDevice = null;
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Successfully disconnected from MIDI device.");

        }

        // ვარიანტი 1: MIDI ნოტის ნომრით
        public void SendNoteOn(int noteNumber, int velocity)
        {

            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} SendNoteOn called - Note: {noteNumber}, Velocity: {velocity}, Enabled: {IsLightControlEnabled}, Connected: {IsConnected}");

            if (!IsLightControlEnabled)
            {
                _logger.LogWarning($"{Environment.NewLine}{DateTime.Now} MIDI control is disabled - skipping note send");
                return;
            }

            if (!IsConnected)
            {
                _logger.LogWarning($"{Environment.NewLine}{DateTime.Now} MIDI device is not connected - skipping note send");
                return;
            }

            if (_midiOutputDevice == null)
            {
                _logger.LogError($"{Environment.NewLine}{DateTime.Now} MIDI output device is null - skipping note send");
                return;
            }

            // Note-ის ობიექტი შექმნა MIDI ნოტის ნომრიდან
            try
            {
                //var note = Melanchall.DryWetMidi.MusicTheory.Note.Get(new SevenBitNumber((byte)noteNumber));
                var noteOn = new NoteOnEvent((SevenBitNumber)noteNumber, (SevenBitNumber)velocity);

                _midiOutputDevice!.SendEvent(noteOn);
                //_midiOutputDevice!.SendEvent(new NoteOnEvent(note.NoteNumber, new SevenBitNumber((byte)velocity)));
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} MIDI NoteOn sent successfully - Note: {noteNumber}, Velocity: {velocity}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} MIDI device Was Not Sent {ex.Message}.");

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
            _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} Sent MIDI event: {e.Event}");
        }

        public void Dispose()
        {
            if (_midiOutputDevice != null)
            {
                _midiOutputDevice.Dispose();
                _midiOutputDevice = null;
                _logger.LogInformation($"{Environment.NewLine}{DateTime.Now} MIDI device disconnected/disposed.");
            }
        }
    }
}