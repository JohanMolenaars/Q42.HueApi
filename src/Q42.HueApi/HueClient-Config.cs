﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Q42.HueApi.Models.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Q42.HueApi
{
  /// <summary>
  /// Partial HueClient, contains requests to the /config/ url
  /// </summary>
  public partial class HueClient
  {
    /// <summary>
    /// Deletes a whitelist entry
    /// </summary>
    /// <returns></returns>
    public async Task<bool> DeleteWhiteListEntryAsync(string entry)
    {
      CheckInitialized();

      HttpClient client = HueClient.GetHttpClient();

      var response = await client.DeleteAsync(new Uri(string.Format("{0}config/whitelist/{1}", ApiBase, entry))).ConfigureAwait(false);
      var stringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

      JArray jresponse = JArray.Parse(stringResponse);
      JObject result = (JObject)jresponse.First;

      JToken error;
      if (result.TryGetValue("error", out error))
      {
        if (error["type"].Value<int>() == 3) // entry not available
          return false;
        else
          throw new Exception(error["description"].Value<string>());
      }

      return true;

    }



    /// <summary>
    /// Asynchronously gets the whitelist with the bridge.
    /// </summary>
    /// <returns>An enumerable of <see cref="WhiteList"/>s registered with the bridge.</returns>
    public async Task<IEnumerable<WhiteList>> GetWhiteListAsync()
    {
      CheckInitialized();

      BridgeConfig config = await GetConfigAsync().ConfigureAwait(false);
      
      return config.WhiteList.Select(l => l.Value).ToList();
    }


    /// <summary>
    /// Get bridge info
    /// </summary>
    /// <returns></returns>
    public async Task<Bridge> GetBridgeAsync()
    {
      CheckInitialized();

      HttpClient client = HueClient.GetHttpClient();
      var stringResult = await client.GetStringAsync(new Uri(ApiBase)).ConfigureAwait(false);

      BridgeState jsonResult = DeserializeResult<BridgeState>(stringResult);

      return new Bridge(jsonResult);
    }
    
    
    /// <summary>
    /// Get bridge config
    /// </summary>
    /// <returns>BridgeConfig object</returns>
    public async Task<BridgeConfig> GetConfigAsync()
    {
        CheckInitialized();

        HttpClient client = HueClient.GetHttpClient();
        string stringResult = await client.GetStringAsync(new Uri(String.Format("{0}config", ApiBase))).ConfigureAwait(false);
        JToken token = JToken.Parse(stringResult);
        BridgeConfig config = null;
        if (token.Type == JTokenType.Object)
        {
            var jsonResult = (JObject)token;
            config = JsonConvert.DeserializeObject<BridgeConfig>(jsonResult.ToString());
        }
        return config;
    }

    /// <summary>
    /// Update bridge config
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public async Task<HueResults> UpdateBridgeConfigAsync(BridgeConfigUpdate update)
    {
      CheckInitialized();

      string command = JsonConvert.SerializeObject(update, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

      HttpClient client = HueClient.GetHttpClient();
      var result = await client.PutAsync(new Uri(string.Format("{0}config", ApiBase)), new StringContent(command)).ConfigureAwait(false);

      string jsonResult = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

      return DeserializeDefaultHueResult(jsonResult);
    }
  }
}
