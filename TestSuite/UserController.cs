using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Threading;
using SharpDX.XInput;

namespace TestSuite
{
    /// <summary>
    /// Represents a single Xbox Controller. Provides and interface to begin/end timing for a
    /// user.
    /// </summary>
    class UserController
    {
        private Controller controller;
        private Gamepad gamepad;

        private bool isConnected;

        private UserIndex userIndex;
        private IUserControllerDelegate controllerDelegate;
        // Dispatch Timer that polls for key press every 10ms (or 200ms if not connected)
        private Timer controllerTimer;

        // Controls the timer for this controller
        private Stopwatch conditionStopwatch;

        /// <summary>
        /// Instantiates a single user xbox controller
        /// </summary>
        /// <param name="userIndex">The index of this controller (1-4)</param>
        /// <param name="controllerDelegate">The delegate that handles the timing methods</param>
        public UserController(UserIndex userIndex, IUserControllerDelegate controllerDelegate)
        {
            this.userIndex = userIndex;
            this.controllerDelegate = controllerDelegate;
            controller = new Controller(userIndex);
            isConnected = controller.IsConnected;

            controllerTimer = new Timer(isConnected ? 10 : 200);
            controllerTimer.Elapsed += CheckState;
            controllerTimer.Start();

            conditionStopwatch = new Stopwatch();
        }

        // Polls the state of the controller to check for button presses/disconnections
        private void CheckState(object sender, ElapsedEventArgs e)
        {
            bool prevIsConnected = isConnected;
            isConnected = controller.IsConnected;

            // Check for change in connected state
            if (!isConnected)
            {
                if (prevIsConnected)
                {
                    controllerTimer.Interval = 200;
                }
                return;
            }
            else if (!prevIsConnected)
            {
                controllerTimer.Interval = 10;
            }

            gamepad = controller.GetState().Gamepad;
            
            // Stop Timer / Confirm User Selection
            if (ButtonsPressed(gamepad.Buttons, GamepadButtonFlags.A))
            {
                controllerDelegate.LetterButtonPressed(userIndex);
            }

            if (conditionStopwatch.IsRunning)
            {
                controllerDelegate.UpdateTimeElapsed(userIndex, conditionStopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Restarts the timer for this controller
        /// </summary>
        public void StartTiming()
        {
            conditionStopwatch.Restart();
        }

        public void StopTiming()
        {
            if (conditionStopwatch.IsRunning)
            {
                conditionStopwatch.Stop();
            }
        }

        public long TimeElapsed()
        {
            return conditionStopwatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Checks if a specific set of buttons has been pressed
        /// </summary>
        /// <param name="gamepadButtonState">The state of the Gamepad Buttons (i.e the actual buttons pressed)</param>
        /// <param name="buttonToCheck">The bitmask of button combinations to check, i.e if at least one of them is pressed</param>
        /// <returns>True if one of the buttonToCheck is pressed. False if none are pressed</returns>
        private bool ButtonsPressed(GamepadButtonFlags gamepadButtonState, GamepadButtonFlags buttonToCheck)
        {
            return (gamepadButtonState & buttonToCheck) != GamepadButtonFlags.None;
        }
        
    }

    /// <summary>
    /// Delegate for the UserController. Handles the Timer Stop and Tick Update
    /// </summary>
    interface IUserControllerDelegate
    {
        void LetterButtonPressed(UserIndex controllerIndex);
        void UpdateTimeElapsed(UserIndex controllerIndex, long elapsedTime);
    }
}
