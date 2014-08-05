using Abstracta.FiddlerSessionComparer;
using Abstracta.FiddlerSessionComparer.Utils;
using Abstracta.Generators.Framework.AbstractGenerator;
using Fiddler;

namespace Abstracta.Generators.Framework.OSTAGenerator
{
    internal class PageRequest : AbstractPageRequest
    {
        internal PageRequest(Session request, AbstractStep myStep, Page page)
            : base(request, myStep, page)
        {
        }
        
        public override string ToString()
        {
            return string.Empty;
        }
    }
}
