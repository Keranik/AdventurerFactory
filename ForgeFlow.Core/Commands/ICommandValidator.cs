namespace ForgeFlow.Core.Commands;

/// <summary>
/// Validates commands before they reach their handler. Registered on <see cref="CommandBus"/>
/// via <see cref="CommandBus.AddValidator"/>. Validators run in registration order; the first
/// non-null result short-circuits dispatch and is returned as the command result.
///
/// Typical uses: tutorial-lock checks, pause-state rejection, cooldown enforcement.
/// </summary>
public interface ICommandValidator
{
    /// <summary>
    /// Validates the given command. Return null to pass validation, or a
    /// <see cref="CommandResult"/> (typically <see cref="CommandResult.Fail"/>) to reject the command.
    /// </summary>
    CommandResult? Validate<T>(T command) where T : IGameCommand;
}
