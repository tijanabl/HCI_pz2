using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
namespace NetworkService.Helpers
{
    public class MyICommand : ICommand
    {
        private Action _targetExecuteMethod;
        private Func<bool> _targetCanExecuteMethod;

        public MyICommand(Action executeMethod)
        {
            _targetExecuteMethod = executeMethod;
        }

        public MyICommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            _targetExecuteMethod = executeMethod;
            _targetCanExecuteMethod = canExecuteMethod;
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged(this, EventArgs.Empty);
        }

        bool ICommand.CanExecute(object parameter)
        {
            if (_targetCanExecuteMethod != null)
                return _targetCanExecuteMethod();
            return _targetExecuteMethod != null;
        }

        public event EventHandler CanExecuteChanged = delegate { };

        void ICommand.Execute(object parameter)
        {
            _targetExecuteMethod?.Invoke();
        }
    }

    public class MyICommand<T> : ICommand
    {
        private Action<T> _targetExecuteMethod;
        private Func<T, bool> _targetCanExecuteMethod;

        public MyICommand(Action<T> executeMethod)
        {
            _targetExecuteMethod = executeMethod;
        }

        public MyICommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            _targetExecuteMethod = executeMethod;
            _targetCanExecuteMethod = canExecuteMethod;
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged(this, EventArgs.Empty);
        }

        bool ICommand.CanExecute(object parameter)
        {
            if (_targetCanExecuteMethod != null)
                return _targetCanExecuteMethod((T)parameter);
            return _targetExecuteMethod != null;
        }

        public event EventHandler CanExecuteChanged = delegate { };

        void ICommand.Execute(object parameter)
        {
            _targetExecuteMethod?.Invoke((T)parameter);
        }
    }
}