using Telegram.Bot.Types;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public interface IStepInputHandler<TInput>
{
    public StepResult GetResult(TInput input);
}