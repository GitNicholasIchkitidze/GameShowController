using GameController.FBService.Extensions;
using GameController.FBService.Heplers;
using GameController.FBService.Services;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

public class RedisGlobalVarsKeeper : IGlobalVarsKeeper
{
	private readonly IConnectionMultiplexer _redisConnection;
	private readonly ILogger<RedisGlobalVarsKeeper> _logger;

	private readonly IDatabase _redis;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly string _prefix;
	private readonly string _channel;

	private readonly TimeSpan? _defaultTtl;
	private readonly ISubscriber _subscriber;

	public event Func<string, string?, Task>? OnChanged;

	public RedisGlobalVarsKeeper(IConnectionMultiplexer redis,
				ILogger<RedisGlobalVarsKeeper> logger,
				string prefix = "GameController:Vars:",	TimeSpan? defaultTtl = null)
	{
		_redisConnection = redis;
		_logger = logger;
		_redis = redis.GetDatabase();
		_prefix = prefix.EndsWith(":") ? prefix : prefix + ":";
		_channel = _prefix + "__changes__";

		_defaultTtl = defaultTtl; // შეიძლება null იყოს => უსასრულოდ ინახება
		_subscriber = redis.GetSubscriber();

		_jsonOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};

		_subscriber.Subscribe(_channel, async (channel, message) =>
		{
			try
			{
				var msg = JsonSerializer.Deserialize<RedisChangeMessage>(message);


				if (msg != null && OnChanged != null)
					await OnChanged.Invoke(msg.Key, msg.Value);
			}
			catch (Exception ex)
			{
				_logger.LogErrorWithCaller($"[RedisGlobalVarsKeeper] Failed to handle change notification: {ex.Message}");
				
			}
		});

	}
	private string GetKey(string key) => $"{_prefix}{key}";


	public async Task SetValueAsync<T>(string key, T value)
	{
		var json = JsonSerializer.Serialize(value, _jsonOptions);
		var redisKey = GetKey(key);
		await _redis.StringSetAsync(redisKey, json, _defaultTtl);
		


		// Publish change notification
		var change = new RedisChangeMessage { Key = key, Value = json };
		var msg = JsonSerializer.Serialize(change);
		await _subscriber.PublishAsync(_channel, msg);

		// Trigger local event too (for same-process updates)
		//if (OnChanged != null)
		//	await OnChanged.Invoke(key, json);
	}

	public async Task<T?> GetValueAsync<T>(string key)
	{



		var val = await _redis.StringGetAsync(GetKey(key));
		if (val.IsNullOrEmpty)
			return default;

		return JsonSerializer.Deserialize<T>(val!, _jsonOptions);
	}

	


	public async Task<bool> ExistsAsync(string key)
	{
		return await _redis.KeyExistsAsync(GetKey(key));

	}

	public async Task RemoveAsync(string key)
	{
		await _redis.KeyDeleteAsync(GetKey(key));
		var change = new RedisChangeMessage { Key = key, Value = null };
		var msg = JsonSerializer.Serialize(change);
		await _subscriber.PublishAsync(_channel, msg);
		if (OnChanged != null)
			await OnChanged.Invoke(key, null);

	}

	/// <summary>
	/// აბრუნებს ყველა Redis key-ს (pattern-ით ან ყველას)
	/// </summary>
	public async Task<IEnumerable<string>> GetAllKeysAsync(string? pattern = null)
	{
		var endpoints = _redisConnection.GetEndPoints();
		var server = _redisConnection.GetServer(endpoints.First());

		// თუ pattern არ არის მითითებული, ყველა key მოიძებნება
		pattern ??= "*";

		var keys = server.Keys(pattern: pattern)
						 .Select(k =>  (string)k)
						 .ToList();

		await Task.CompletedTask; // async სიგნატურის შესანარჩუნებლად
		return keys;
	}

	// <summary>
	/// აბრუნებს ყველა Redis key-ს (pattern-ით ან ყველას) mniSvnelobebTan erTad
	/// </summary>
	public async Task<Dictionary<string, string>> GetAllKeysAndValuesAsync(string? pattern = null)
	{
		// 1. Redis სერვერთან და კავშირთან დაკავშირება
		var endpoints = _redisConnection.GetEndPoints();
		var server = _redisConnection.GetServer(endpoints.First());
		var db = _redisConnection.GetDatabase(); // IDatabase ობიექტი მონაცემების წასაკითხად

		// თუ pattern არ არის მითითებული, ყველა key მოიძებნება
		pattern ??= "*";

		// 2. ქეიების მოძიება სერვერიდან
		var keys = server.Keys(pattern: pattern)
						 .Select(k => (RedisKey)k) // RedisKey ტიპზე გადაყვანა
						 .ToArray();

		// თუ ქეიები არ მოიძებნა, ცარიელი ლექსიკონი დააბრუნეთ
		if (keys.Length == 0)
		{
			return new Dictionary<string, string>();
		}

		// 3. მნიშვნელობების მიღება MGET ბრძანების გამოყენებით
		// StringGetAsync მრავალ RedisKey-ს იღებს და RedisValue-ების მასივს აბრუნებს
		var values = await db.StringGetAsync(keys);

		// 4. შედეგის Dictionary<string, string>-ში გაერთიანება
		var results = new Dictionary<string, string>();

		for (int i = 0; i < keys.Length; i++)
		{
			// დარწმუნდით, რომ ქეი არსებობს და მნიშვნელობა არ არის null (ანუ იპოვნა)
			if (values[i].HasValue)
			{
				// RedisKey და RedisValue-ის string-ად გადაყვანა
				results.Add(keys[i].ToString(), values[i].ToString());
			}
		}

		return results;
	}


	/// <summary>
	/// შლის ყველა Redis key-ს (pattern-ით ან ყველას)
	/// </summary>
	public async Task ClearAllAsync(string? pattern = null)
	{
		var endpoints = _redisConnection.GetEndPoints();
		var server = _redisConnection.GetServer(endpoints.First());
		pattern ??= "*";

		var keys = server.Keys(pattern: pattern).ToArray();
		if (keys.Length > 0)
			await _redis.KeyDeleteAsync(keys);
	}
	public async ValueTask DisposeAsync()
	{
		await _subscriber.UnsubscribeAsync(_channel);
	}

	private class RedisChangeMessage
	{
		public string Key { get; set; } = "";
		public string? Value { get; set; }
	}
}
