using BugReport.Anomaly;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class AnomalyMotorTests : PlayModeTestBase
    {
        private const float Dt = 1f / 60f;

        private AnomalyMotor CreateMotor(Vector3 position)
        {
            var go = Track(new GameObject("Anomaly"));
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.radius = 0.3f;
            cc.height = 0.6f;
            cc.stepOffset = 0.1f;
            var motor = go.AddComponent<AnomalyMotor>();
            Physics.SyncTransforms();
            return motor;
        }

        private static void Run(AnomalyMotor motor, Vector2 planar, float vertical, int frames, bool boost = false)
        {
            for (int i = 0; i < frames; i++) motor.Tick(planar, vertical, boost, Quaternion.identity, Dt);
        }

        [Test]
        public void ForwardInput_MovesAlongBasisForward()
        {
            var motor = CreateMotor(new Vector3(0f, 2f, 0f));
            Run(motor, Vector2.up, 0f, 60);

            var p = motor.transform.position;
            Assert.That(p.z, Is.GreaterThan(2f));
            Assert.That(Mathf.Abs(p.x), Is.LessThan(0.01f));
            Assert.That(p.y, Is.EqualTo(2f).Within(0.01f), "no gravity on the anomaly");
        }

        [Test]
        public void Basis_RotatesMovementAndIgnoresPitch()
        {
            var motor = CreateMotor(new Vector3(0f, 2f, 0f));
            var basis = Quaternion.Euler(60f, 90f, 0f); // looking right and steeply down
            for (int i = 0; i < 60; i++) motor.Tick(Vector2.up, 0f, false, basis, Dt);

            var p = motor.transform.position;
            Assert.That(p.x, Is.GreaterThan(2f));
            Assert.That(p.y, Is.EqualTo(2f).Within(0.01f));
        }

        [Test]
        public void VerticalInput_Ascends()
        {
            var motor = CreateMotor(new Vector3(0f, 2f, 0f));
            Run(motor, Vector2.zero, 1f, 30);
            Assert.That(motor.transform.position.y, Is.GreaterThan(2.5f));
        }

        [Test]
        public void Speed_NeverExceedsMaxWithoutBoost()
        {
            var motor = CreateMotor(new Vector3(0f, 2f, 0f));
            Run(motor, new Vector2(5f, 5f), 0f, 120); // over-length input is clamped
            Assert.That(motor.Speed, Is.LessThanOrEqualTo(motor.MaxSpeed + 1e-3f));
        }

        [Test]
        public void Boost_IncreasesSpeed()
        {
            var motor = CreateMotor(new Vector3(0f, 2f, 0f));
            Run(motor, Vector2.up, 0f, 120, boost: true);
            Assert.That(motor.Speed, Is.GreaterThan(motor.MaxSpeed));
        }

        [Test]
        public void NoInput_DeceleratesToRest()
        {
            var motor = CreateMotor(new Vector3(0f, 2f, 0f));
            Run(motor, Vector2.up, 0f, 60);
            Run(motor, Vector2.zero, 0f, 120);
            Assert.That(motor.Speed, Is.LessThan(1e-3f));
        }

        [Test]
        public void Wall_BlocksMovementAndDoesNotAccumulateVelocity()
        {
            var motor = CreateMotor(new Vector3(0f, 2f, 0f));
            Box("Wall", new Vector3(0f, 2f, 3f), new Vector3(10f, 10f, 0.5f));
            Physics.SyncTransforms();

            Run(motor, Vector2.up, 0f, 240);

            Assert.That(motor.transform.position.z, Is.LessThan(3f - 0.25f), "must stay in front of the wall");
            Assert.That(motor.Velocity.z, Is.LessThan(1f), "velocity into the wall must not build up");
        }

        [Test]
        public void ZeroOrNegativeDeltaTime_DoesNothing()
        {
            var motor = CreateMotor(new Vector3(0f, 2f, 0f));
            motor.Tick(Vector2.up, 0f, false, Quaternion.identity, 0f);
            motor.Tick(Vector2.up, 0f, false, Quaternion.identity, -1f);
            Assert.That(motor.transform.position, Is.EqualTo(new Vector3(0f, 2f, 0f)));
        }
    }
}
