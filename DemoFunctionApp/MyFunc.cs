using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace DemoFunctionApp;

public class MyFunc(TimeProvider timeProvider)
{
    // HTTP �g���K�[�Ŏ󂯎�����f�[�^���L���[�ɒǉ�
    [Function(nameof(AddData))]
    public AddDataOutputs AddData(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] InputData[] input) => 
        new(new AcceptedResult(), input);

    // �L���[����f�[�^�����o���ă`�F�b�N���A�e�[�u���ɕۑ�
    // �`�F�b�N�Ɉ������������ꍇ�� SignalR �Œʒm
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

    // SignalR �ւ̐ڑ�����Ԃ�
    [Function(nameof(Negotiate))]
    public static IActionResult Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
        [SignalRConnectionInfoInput(HubName = nameof(MyFunc))] string connectionInfo) =>
        new ContentResult { ContentType = "application/json", Content = connectionInfo };
}

// Http �g���K�[�Ŏ󂯎��f�[�^
public record InputData(int Value);

// �֐��̏o��
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
