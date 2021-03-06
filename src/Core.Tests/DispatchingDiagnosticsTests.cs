using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace GreenDonut
{
    public class DispatchingDiagnosticsTests
    {
        [Fact(DisplayName = "ExecuteBatchRequest: Should record a batch request plus error",
            Skip = "Due to concurrency issues in the test listener")]
        public async Task ExecuteBatchRequest()
        {
            var listener = new DispatchingListener();
            var observer = new DispatchingObserver(listener);

            using (DiagnosticListener.AllListeners.Subscribe(observer))
            {
                // arrange
                FetchDataDelegate<string, string> fetch = async keys =>
                {
                    var error = new Exception("Quux");

                    return await Task.FromResult(new[]
                    {
                        Result<string>.Reject(error)
                    }).ConfigureAwait(false);
                };
                var options = new DataLoaderOptions<string>();
                var loader = new DataLoader<string, string>(options, fetch);

                // act
                try
                {
                    await loader.LoadAsync("Foo").ConfigureAwait(false);
                }
                catch
                {
                }

                // assert
                Assert.Collection(listener.Keys,
                    (key) => Assert.Equal("Foo", key));
                Assert.Collection(listener.Values,
                    (item) =>
                    {
                        Assert.Equal("Foo", item.Key);
                        Assert.True(item.Value.IsError);
                        Assert.Equal("Quux", item.Value.Error.Message);
                    });
                Assert.Collection(listener.Errors,
                    (item) =>
                    {
                        Assert.Equal("Foo", item.Key);
                        Assert.Equal("Quux", item.Value.Message);
                    });
            }
        }
    }
}
