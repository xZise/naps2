using Eto.Forms;

namespace NAPS2.EtoForms;

public class ActionCommand : Command
{
    public ActionCommand()
    {
    }

    public ActionCommand(Action action) : base((sender, args) => action())
    {
    }

    public ActionCommand(Func<Task> action) : base(async (sender, args) => await action())
    {
    }

    public string? MacSymbol { get; set; }
}