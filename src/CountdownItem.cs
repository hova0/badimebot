using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace badimebot
{
    public struct CountdownItem
    {
        public TimeSpan PreCountdown;
        public TimeSpan Length;
        public string Title;
        public DateTime Epoch;
        public static CountdownItem Empty = new CountdownItem();

        public static bool operator ==(CountdownItem item1, CountdownItem item2)
        {
            return item1.Title == item2.Title && item1.Length == item2.Length;
        }
        public static bool operator !=(CountdownItem item1, CountdownItem item2)
        {
            return item1.Title != item2.Title && item1.Length != item2.Length;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return this.Title.GetHashCode() ^ this.Length.GetHashCode() ^ this.PreCountdown.GetHashCode();
        }

        public CountdownItem Copy()
        {
            CountdownItem i = new CountdownItem();
            i.Title = this.Title;
            i.Length = this.Length;
            i.PreCountdown = this.PreCountdown;
            i.Epoch = this.Epoch;
            return i;
        }
    }
}
