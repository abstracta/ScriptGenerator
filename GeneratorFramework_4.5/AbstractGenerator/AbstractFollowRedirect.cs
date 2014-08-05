using Abstracta.FiddlerSessionComparer;

namespace Abstracta.Generators.Framework.AbstractGenerator
{
    using Fiddler;
    
    internal class AbstractFollowRedirect : HTTPRequest
    {
        public AbstractFollowRedirect(Session request, RedirectType rType, Page page) : base(request, page)
        {
            RedirectType = rType;
        }

        internal RedirectType RedirectType { get; set; }
    }
}
