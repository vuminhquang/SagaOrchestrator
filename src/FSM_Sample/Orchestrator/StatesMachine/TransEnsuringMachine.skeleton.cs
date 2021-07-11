using System;
using Appccelerate.StateMachine;
using Appccelerate.StateMachine.AsyncMachine;

namespace Orchestrator
{
    public partial class TransEnsuringMachine
    {
        //flag, = States.Error while Rolling Back from step to step (backward direction)
        //After rolling back done, this will = States.Healthy (forward direction)
        private States _direction = States.Healthy;

        private void DefineStatesAndTransitions(StateMachineDefinitionBuilder<States, Events> builder)
        {
            DefineHealthyStates(builder);

            HandleErrors(builder);

            HandleActions(builder);

            builder.In(States.Off)
                .ExecuteOnEntry(() =>
                {
                    Console.WriteLine("Off");
                })
                .On(Events.NextStep)
                .Goto(States.Step1)
                .Execute(() =>
                {
                    _direction = States.Healthy;
                });

            builder.In(States.Complete)
                .ExecuteOnEntry(async () =>
                {
                    Console.WriteLine("Complete");
                    await _machine.Fire(Events.NextStep);
                })
                .On(Events.NextStep)
                .Goto(States.Off)
                .Execute(() =>
                {
                    _direction = States.Healthy;
                });

            builder.WithInitialState(States.Off);
        }

        private static void DefineHealthyStates(StateMachineDefinitionBuilder<States, Events> builder)
        {
            builder.DefineHierarchyOn(States.Healthy)
                .WithHistoryType(HistoryType.Deep)//Important, will return to last sub-states after coming back from error state
                .WithInitialSubState(States.Off)
                .WithSubState(States.Step1)
                .WithSubState(States.Step2);
        }

        private void HandleErrors(StateMachineDefinitionBuilder<States, Events> builder)
        {
            builder.In(States.Healthy)
                .On(Events.Error)
                .Goto(States.Error)
                .Execute(async () =>
                {
                    _direction = States.Error;
                    await _machine.Fire(Events.RollBack);
                })
                ;

            builder.In(States.Error)
                .On(Events.RollBack).Goto(States.Healthy)//Go back to the last healthy sub-state to do the rollback
                .On(Events.Error);
        }
    }
}
