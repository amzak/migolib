using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using ReactiveUI;

namespace MigoToolGui.Dialogs
{
    public class OpenFileDialogHelper : AvaloniaObject, ICommandSource
    {
        private static readonly AvaloniaProperty<string> FileNameProperty =
            AvaloniaProperty.Register<OpenFileDialogHelper, string>("FileName");
        
        public string FileName
        {
            get => this.GetValue<string>(FileNameProperty);
            set => this.SetValue<string>(FileNameProperty, value);
        }
        
        public ReactiveCommand<Unit, Unit> ShowDialog { get; private set; }
        
        public static readonly DirectProperty<OpenFileDialogHelper, ICommand> CommandProperty =
            AvaloniaProperty.RegisterDirect<OpenFileDialogHelper, ICommand>(
                nameof(Command),
                dialogHelper => dialogHelper.Command, 
                (dialogHelper, command) =>
                {
                    if (command == default)
                    {
                        return;
                    }
                    
                    dialogHelper.Command = command;
                }, enableDataValidation: true);
        
        private ICommand _fileSelectedCommand;

        public void CanExecuteChanged(object sender, EventArgs e)
        {
        }

        public ICommand Command
        {
            get => _fileSelectedCommand;
            set => SetAndRaise(CommandProperty, ref _fileSelectedCommand, value);
        }

        public object CommandParameter { get; }

        public bool IsEffectivelyEnabled => true;
        
        public OpenFileDialogHelper()
        {
            CommandParameter = new object();
            _fileSelectedCommand = ReactiveCommand.Create<string, Unit>(_ => Unit.Default);
            ShowDialog = ReactiveCommand.CreateFromTask(ShowDialogAction);
        }

        private async Task ShowDialogAction()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var openFileDialog = new OpenFileDialog
                {
                    AllowMultiple = false                    
                };
                var files = await openFileDialog.ShowAsync(desktop.MainWindow);

                if (files.Length != 1)
                {
                    return;
                }

                if (_fileSelectedCommand != null)
                {
                    _fileSelectedCommand.Execute(files[0]);
                }
            }
        }
    }
}