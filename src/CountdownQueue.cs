using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace badimebot
{
    public class CountdownQueue<T> : System.Collections.Generic.List<T>
    {
       public T Dequeue()
        {
            T item = this[0];
            this.RemoveAt(0);
            return item;
        }

        public void Enqueue(T item)
        {
            this.Add(item);
        }
    }
}
