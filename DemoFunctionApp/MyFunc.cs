using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace DemoFunctionApp;

public class MyFunc(TimeProvider timeProvider)
{
    // HTTP トリガーで受け取ったデータをキューに追加
    [Function(nameof(AddData))]
    public AddDataOutputs AddData(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] InputData[] input) => 
        new(new AcceptedResult(), input);

    // キューからデータを取り出してチェックし、テーブルに保存
    // チェックに引っかかった場合は SignalR で通知
    [Function(nameof(CheckAndStoreData))]
    public CheckAndStoreDataOutputs CheckAndStoreData(
        [QueueTrigger("data-queue")] InputData data)
    {
        var storeData = new TableEntity
        {
            PartitionKey = timeProvider.GetUtcNow().ToString("yyyy-MM-dd"),
            RowKey = Guid.NewGuid().ToString(),
            ["Value"] = data.Value,
            ["IsValid"] = data.Value < 100,
        };
        return new(storeData,
            (bool)storeData["IsValid"] ? null : new SignalRMessageAction("Alert", [storeData]));
    }

    // SignalR への接続情報を返す
    [Function(nameof(Negotiate))]
    public static IActionResult Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
        [SignalRConnectionInfoInput(HubName = nameof(MyFunc))] string connectionInfo) =>
        new ContentResult { ContentType = "application/json", Content = connectionInfo };
}

// Http トリガーで受け取るデータ
public record InputData(int Value);

// 関数の出力
public record AddDataOutputs(
    [property: HttpResult]
    IActionResult Response,
    [property: QueueOutput("data-queue")]
    IEnumerable<InputData> QueueOutput);

public record CheckAndStoreDataOutputs(
    [property:TableOutput("outputdatademo1")] 
    TableEntity TableOutput,
    [property:SignalROutput(HubName = nameof(MyFunc))] 
    SignalRMessageAction? SignalRMessageAction);
