using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GameController.Shared.Models
{
    public class TcpListenModel : INotifyPropertyChanged
    {
        private bool _acceptingAnswers;

        public bool AcceptingAnswers
        {
            get => _acceptingAnswers;
            set
            {
                if (_acceptingAnswers != value)
                {
                    _acceptingAnswers = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText)); // აცნობებს UI-ს, რომ StatusText შეიცვალა

                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public string StatusText => AcceptingAnswers ? "პასუხების მიღება ჩართულია" : "პასუხების მიღება გამორთულია";


        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
