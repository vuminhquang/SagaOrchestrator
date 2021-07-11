using System;
using System.Threading.Tasks;
using Appccelerate.StateMachine.AsyncMachine;

namespace Orchestrator
{
    public partial class TransEnsuringMachine
    {
        public Func<Task> Step1 { get; set; }//Not use Action here since we want to async
        public Func<Task> Step2 { get; set; }
        public Func<Task> RollbackStep1 { get; set; }
        public Func<Task> RollbackStep2 { get; set; }


        private void HandleActions(StateMachineDefinitionBuilder<States, Events> builder)
        {
            //Important: allow to go to ExecuteOnEntry when transit to from Error state
            // builder.In(States.Step1).On(Events.RollBack);
            // builder.In(States.Step2).On(Events.RollBack);

            builder.In(States.Step1)
                .ExecuteOnEntry(async () =>
                {
                    if (_direction == States.Healthy)
                    {
                        try
                        {
                            await Step1();
                            await _machine.Fire(Events.NextStep);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            await _machine.Fire(Events.Error);
                        }
                    }
                    else if (_direction == States.Error)
                    {
                        await _machine.Fire(Events.RollBack);//To rollback to step 1
                    }
                })
                .On(Events.NextStep)
                .Goto(States.Step2)
                .On(Events.RollBack)
                .Goto(States.Complete)
                .Execute(async () =>
                {
                    await RollbackStep1();
                });

            builder.In(States.Step2)
                .ExecuteOnEntry(async () =>
                {
                    if (_direction == States.Healthy)
                    {
                        try
                        {
                            await Step2();
                            await _machine.Fire(Events.NextStep);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            await _machine.Fire(Events.Error);
                        }
                    }
                    else if (_direction == States.Error)
                    {
                        await _machine.Fire(Events.RollBack);//To rollback to step 1
                    }
                })
                .On(Events.NextStep)
                .Goto(States.Complete)
                .On(Events.RollBack)
                .Goto(States.Step1)
                .Execute(async () =>
                {
                    await RollbackStep2();
                });
        }

    }
}
