using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace NetworkService.Model
{
    public class EntityTypeGroup
    {
        public string TypeName { get; set; }
        public ObservableCollection<EntityDisplayItem> Entities { get; set; }
    }

    public class EntityDisplayItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }
    }
}