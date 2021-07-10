
using System.Threading.Tasks;
using Appccelerate.StateMachine;
using Appccelerate.StateMachine.AsyncMachine;

namespace Orchestrator
{
    public partial class TransEnsuringMachine
    {
        private readonly AsyncPassiveStateMachine<States, Events> _machine;

        private enum States
        {
            Healthy,
            Off,
            Step1,
            Step2,
            Complete,
            Error
        }

        private enum Events
        {
            NextStep,
            RollBack,
            Error
        }

        public TransEnsuringMachine()
        {
            var builder = new StateMachineDefinitionBuilder<States, Events>();

            DefineStatesAndTransitions(builder);

            var definition = builder.Build();

            _machine = definition
                .CreatePassiveStateMachine("StatesMachine");

            _machine.Start();
        }

        public async Task Error()
        {
            await _machine.Fire(Events.Error);
        }

        public async Task Start()
        {
            await _machine.Fire(Events.NextStep);
        }
    }
}
