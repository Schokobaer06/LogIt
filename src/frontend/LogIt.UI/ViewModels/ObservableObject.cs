using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LogIt.UI.ViewModels
{
    /// <summary>
    /// - Basisklasse für ViewModels in WPF
    /// - Implementiert INotifyPropertyChanged für Property-Benachrichtigungen
    /// </summary>
    public class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// - Event für Property-Änderungen
        /// - Wird ausgelöst, wenn sich eine Eigenschaft ändert
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// - Löst das PropertyChanged-Event aus
        /// - Informiert die UI über Änderungen an Properties
        /// - propertyName: Name der geänderten Eigenschaft (automatisch über CallerMemberName)
        /// </summary>
        /// <param name="propertyName">Name der geänderten Eigenschaft</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
