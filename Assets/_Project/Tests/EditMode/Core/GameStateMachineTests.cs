using System.Collections.Generic;
using BugReport.Core.GameFlow;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class GameStateMachineTests
    {
        [Test]
        public void StartsInGivenState()
        {
            Assert.That(new GameStateMachine().Current, Is.EqualTo(GameState.Boot));
            Assert.That(new GameStateMachine(GameState.Playing).Current, Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void AllowedTransition_ChangesStateAndRaises()
        {
            var machine = new GameStateMachine();
            var events = new List<(GameState, GameState)>();
            machine.StateChanged += (f, t) => events.Add((f, t));

            Assert.That(machine.TryTransition(GameState.Playing), Is.True);

            Assert.That(machine.Current, Is.EqualTo(GameState.Playing));
            Assert.That(events, Is.EqualTo(new[] { (GameState.Boot, GameState.Playing) }));
        }

        [Test]
        public void DisallowedTransition_IsRejected()
        {
            var machine = new GameStateMachine(GameState.MainMenu);
            int events = 0;
            machine.StateChanged += (_, __) => events++;

            Assert.That(machine.TryTransition(GameState.Inspection), Is.False);
            Assert.That(machine.TryTransition(GameState.MainMenu), Is.False, "self transition");
            Assert.That(machine.Current, Is.EqualTo(GameState.MainMenu));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void TogglePause_ResumesIntoInspectionIfPausedFromInspection()
        {
            var machine = new GameStateMachine(GameState.Playing);
            machine.ToggleInspection();
            Assert.That(machine.Current, Is.EqualTo(GameState.Inspection));

            Assert.That(machine.TogglePause(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(GameState.Paused));
            Assert.That(machine.IsGameplayActive, Is.False);

            Assert.That(machine.TogglePause(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(GameState.Inspection));
        }

        [Test]
        public void TogglePause_ResumesIntoPlaying()
        {
            var machine = new GameStateMachine(GameState.Playing);
            machine.TogglePause();
            machine.TogglePause();
            Assert.That(machine.Current, Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void ToggleInspection_OnlyWorksDuringGameplay()
        {
            var machine = new GameStateMachine(GameState.Playing);
            Assert.That(machine.ToggleInspection(), Is.True);
            Assert.That(machine.ToggleInspection(), Is.True);
            Assert.That(machine.Current, Is.EqualTo(GameState.Playing));

            machine.TogglePause();
            Assert.That(machine.ToggleInspection(), Is.False);
            Assert.That(machine.Current, Is.EqualTo(GameState.Paused));
        }

        [Test]
        public void TogglePause_FromMenu_IsRejected()
        {
            var machine = new GameStateMachine(GameState.MainMenu);
            Assert.That(machine.TogglePause(), Is.False);
        }

        [Test]
        public void EveryStateCanEventuallyReachPlaying()
        {
            // Guards against adding a state with no way out (a softlock in the flow graph).
            foreach (GameState start in System.Enum.GetValues(typeof(GameState)))
            {
                var visited = new HashSet<GameState> { start };
                var frontier = new Queue<GameState>();
                frontier.Enqueue(start);
                while (frontier.Count > 0)
                {
                    var s = frontier.Dequeue();
                    foreach (GameState next in System.Enum.GetValues(typeof(GameState)))
                    {
                        if (GameStateMachine.IsTransitionAllowed(s, next) && visited.Add(next)) frontier.Enqueue(next);
                    }
                }
                Assert.That(visited, Does.Contain(GameState.Playing), start + " cannot reach Playing");
            }
        }
    }
}
