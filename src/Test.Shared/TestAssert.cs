namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Minimal assertion helpers for shared tests.  Failures throw <see cref="TestAssertionException"/>.
    /// </summary>
    public static class TestAssert
    {
        /// <summary>
        /// Assert that a condition is true.
        /// </summary>
        /// <param name="condition">Condition.</param>
        /// <param name="message">Failure message.</param>
        /// <exception cref="TestAssertionException">Condition is false.</exception>
        public static void True(bool condition, string message)
        {
            if (!condition) throw new TestAssertionException(message);
        }

        /// <summary>
        /// Assert that a condition is false.
        /// </summary>
        /// <param name="condition">Condition.</param>
        /// <param name="message">Failure message.</param>
        /// <exception cref="TestAssertionException">Condition is true.</exception>
        public static void False(bool condition, string message)
        {
            if (condition) throw new TestAssertionException(message);
        }

        /// <summary>
        /// Assert that two values are equal.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="expected">Expected value.</param>
        /// <param name="actual">Actual value.</param>
        /// <param name="what">Description of the value being compared.</param>
        /// <exception cref="TestAssertionException">Values differ.</exception>
        public static void Equal<T>(T expected, T actual, string what)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new TestAssertionException(what + ": expected '" + expected + "' but was '" + actual + "'");
        }

        /// <summary>
        /// Assert that a value is null.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="what">Description of the value.</param>
        /// <exception cref="TestAssertionException">Value is not null.</exception>
        public static void Null(object? value, string what)
        {
            if (value != null) throw new TestAssertionException(what + ": expected null but was '" + value + "'");
        }

        /// <summary>
        /// Assert that a value is not null.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="what">Description of the value.</param>
        /// <exception cref="TestAssertionException">Value is null.</exception>
        public static void NotNull(object? value, string what)
        {
            if (value == null) throw new TestAssertionException(what + ": expected a value but was null");
        }

        /// <summary>
        /// Assert that an action throws an exception of the specified type (or a derived type).
        /// </summary>
        /// <typeparam name="T">Expected exception type.</typeparam>
        /// <param name="action">Action.</param>
        /// <returns>The thrown exception.</returns>
        /// <exception cref="TestAssertionException">No exception, or an exception of a different type, was thrown.</exception>
        public static T Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T e)
            {
                return e;
            }
            catch (Exception e)
            {
                throw new TestAssertionException("Expected " + typeof(T).Name + " but caught " + e.GetType().Name + ": " + e.Message);
            }

            throw new TestAssertionException("Expected " + typeof(T).Name + " but no exception was thrown");
        }

        /// <summary>
        /// Assert that an asynchronous action throws an exception of the specified type (or a derived type).
        /// </summary>
        /// <typeparam name="T">Expected exception type.</typeparam>
        /// <param name="action">Asynchronous action.</param>
        /// <returns>The thrown exception.</returns>
        /// <exception cref="TestAssertionException">No exception, or an exception of a different type, was thrown.</exception>
        public static async Task<T> ThrowsAsync<T>(Func<Task> action) where T : Exception
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (T e)
            {
                return e;
            }
            catch (Exception e)
            {
                throw new TestAssertionException("Expected " + typeof(T).Name + " but caught " + e.GetType().Name + ": " + e.Message);
            }

            throw new TestAssertionException("Expected " + typeof(T).Name + " but no exception was thrown");
        }

        /// <summary>
        /// Assert that an <see cref="ArgumentNullException"/> is thrown for the named parameter.
        /// </summary>
        /// <param name="action">Action.</param>
        /// <param name="paramName">Expected parameter name.</param>
        /// <exception cref="TestAssertionException">Assertion failed.</exception>
        public static void ThrowsArgumentNull(Action action, string paramName)
        {
            ArgumentNullException e = Throws<ArgumentNullException>(action);
            Equal(paramName, e.ParamName, "ArgumentNullException.ParamName");
        }

        /// <summary>
        /// Assert that an <see cref="ArgumentNullException"/> is thrown asynchronously for the named parameter.
        /// </summary>
        /// <param name="action">Asynchronous action.</param>
        /// <param name="paramName">Expected parameter name.</param>
        /// <returns>Task.</returns>
        /// <exception cref="TestAssertionException">Assertion failed.</exception>
        public static async Task ThrowsArgumentNullAsync(Func<Task> action, string paramName)
        {
            ArgumentNullException e = await ThrowsAsync<ArgumentNullException>(action).ConfigureAwait(false);
            Equal(paramName, e.ParamName, "ArgumentNullException.ParamName");
        }
    }
}
