using Melanchall.DryWetMidi.MusicTheory;
using System;

namespace GameController.Server.Services
{
	// ინტერფეისი, რომელიც განსაზღვრავს MIDI განათების სერვისის ფუნქციონალს
	public interface IMidiLightingService
	{
		bool IsConnected { get; }
		event EventHandler<bool> ConnectionStatusChanged;
		bool IsLightControlEnabled { get; set; }

		void Connect();
		void Disconnect();

		// აგზავნის MIDI Note On შეტყობინებას
		void SendNoteOn(NoteName noteName, Octave octave, int velocity);

		// აგზავნის MIDI Note Off შეტყობინებას
		void SendNoteOff(NoteName noteName, Octave octave);

		// ვარიანტი 1: MIDI ნოტის ნომრით
		void SendNoteOn(int noteNumber, int velocity);
		void SendNoteOff(int noteNumber);

		// დაამატეთ მეთოდი MIDI მოწყობილობასთან კავშირის სტატუსის შესამოწმებლად

	}
}