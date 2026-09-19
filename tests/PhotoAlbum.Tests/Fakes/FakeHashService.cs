namespace PhotoAlbum.Tests.Fakes;
using PhotoAlbum.Application.Abstractions;

//内存版哈希服务：默认按路径生成哈希，可自定义以模拟"内容相同"
public class FakeHashService : IHashService
{
    public Func<string, string> HashFactory { get; set; } = path => "HASH_" + path;

    public Task<string> ComputeHashAsync(string path) => Task.FromResult(HashFactory(path));
}
