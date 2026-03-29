namespace Pr2.ModulesAndDi.Core;

public interface IAppAction
{
    string Title { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
