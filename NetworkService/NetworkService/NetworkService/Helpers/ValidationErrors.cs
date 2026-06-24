using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkService.Helpers
{ 
    public class ValidationErrors : BindableBase
    {
        private readonly Dictionary<string, string> validationErrors
            = new Dictionary<string, string>();

        public bool IsValid => validationErrors.Count < 1;

        public string this[string fieldName]
        {
            get
            {
                return validationErrors.ContainsKey(fieldName)
                    ? validationErrors[fieldName]
                    : string.Empty;
            }
            set
            {
                if (validationErrors.ContainsKey(fieldName))
                {
                    if (string.IsNullOrWhiteSpace(value))
                        validationErrors.Remove(fieldName);
                    else
                        validationErrors[fieldName] = value;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        validationErrors.Add(fieldName, value);
                }
                this.OnPropertyChanged("IsValid");
            }
        }

        public void Clear()
        {
            validationErrors.Clear();
        }
    }
}
