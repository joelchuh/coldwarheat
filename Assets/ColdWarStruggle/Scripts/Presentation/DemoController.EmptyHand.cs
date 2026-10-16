using System.Threading.Tasks;
using ColdWarStruggle.Application;
using ColdWarStruggle.Core;
namespace ColdWarStruggle.Presentation
{
    public sealed partial class DemoController
    {
        public bool CanSkipEmptyHand => ReadView().Practice?.CanSkipEmptyHand==true;
        public Task<CardActionResult> SkipEmptyHandAsync()=>SendCard(CardActionKind.SkipEmptyHand);
    }
}
