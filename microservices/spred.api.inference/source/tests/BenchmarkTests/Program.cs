using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using InferenceService.Helpers;

var summary = BenchmarkRunner.Run<AudioProcessingBenchmark>();
Console.WriteLine(summary);

[MemoryDiagnoser]
public class AudioProcessingBenchmark
{
    private readonly string filePath =
        "D:\\Fork\\Spred\\spred.api\\microservices\\spred.api.inference\\source\\Inference\\AudioFiles\\0cqgpque.qrs";

    private Stream _inputStream;

    [GlobalSetup]
    public void Setup()
    {
        _inputStream = File.OpenRead(filePath);
    }

    [Benchmark]
    public async Task ProcessAudio()
    {
        var path = await ByteFileReader.SaveFile(_inputStream, null);
        //var result = await WaveFormatHelper.GenerateWaveformAsync(path);
    }
}