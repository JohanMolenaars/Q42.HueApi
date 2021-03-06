﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Q42.HueApi.Extensions;
using Newtonsoft.Json;
using Q42.HueApi.Models.Groups;
using System.Dynamic;

namespace Q42.HueApi
{
  /// <summary>
  /// Partial HueClient, contains requests to the /lights/ url
  /// </summary>
  public partial class HueClient
  {
    /// <summary>
    /// Asynchronously retrieves an individual light.
    /// </summary>
    /// <param name="id">The light's Id.</param>
    /// <returns>The <see cref="Light"/> if found, <c>null</c> if not.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or a blank string.</exception>
    public async Task<Light> GetLightAsync(string id)
    {
      if (id == null)
        throw new ArgumentNullException("id");
      if (id.Trim() == String.Empty)
        throw new ArgumentException("id can not be empty or a blank string", "id");

      CheckInitialized();

      HttpClient client = HueClient.GetHttpClient();
      string stringResult = await client.GetStringAsync(new Uri(String.Format("{0}lights/{1}", ApiBase, id))).ConfigureAwait(false);

#if DEBUG
      //Normal result example
      stringResult = "{    \"state\": {        \"hue\": 50000,        \"on\": true,        \"effect\": \"none\",        \"alert\": \"none\",       \"bri\": 200,        \"sat\": 200,        \"ct\": 500,        \"xy\": [0.5, 0.5],        \"reachable\": true,       \"colormode\": \"hs\"    },    \"type\": \"Living Colors\",    \"name\": \"LC 1\",    \"modelid\": \"LC0015\",    \"swversion\": \"1.0.3\",    \"pointsymbol\": {        \"1\": \"none\",        \"2\": \"none\",        \"3\": \"none\",        \"4\": \"none\",        \"5\": \"none\",        \"6\": \"none\",        \"7\": \"none\",        \"8\": \"none\"    }}";
      
      //Lux result
      stringResult = "{    \"state\": {       \"on\": true,        \"effect\": \"none\",        \"alert\": \"none\",       \"bri\": 200,           \"reachable\": true,       \"colormode\": \"hs\"    },    \"type\": \"Living Colors\",    \"name\": \"LC 1\",    \"modelid\": \"LC0015\",    \"swversion\": \"1.0.3\",    \"pointsymbol\": {        \"1\": \"none\",        \"2\": \"none\",        \"3\": \"none\",        \"4\": \"none\",        \"5\": \"none\",        \"6\": \"none\",        \"7\": \"none\",        \"8\": \"none\"    }}";
#endif

      JToken token = JToken.Parse(stringResult);
      if (token.Type == JTokenType.Array)
      {
        // Hue gives back errors in an array for this request
        JObject error = (JObject)token.First["error"];
        if (error["type"].Value<int>() == 3) // Light not found
          return null;

        throw new Exception(error["description"].Value<string>());
      }

      return token.ToObject<Light>();
    }

    /// <summary>
    /// Sets the light name
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task<HueResults> SetLightNameAsync(string id, string name)
    {
      if (id == null)
        throw new ArgumentNullException("id");
      if (id.Trim() == String.Empty)
        throw new ArgumentException("id can not be empty or a blank string", "id");

      CheckInitialized();

      string command = JsonConvert.SerializeObject(new { name = name});

      HttpClient client = HueClient.GetHttpClient();
      var result = await client.PutAsync(new Uri(String.Format("{0}lights/{1}", ApiBase, id)), new StringContent(command)).ConfigureAwait(false);

      var jsonResult = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

      return DeserializeDefaultHueResult(jsonResult);
    }

    /// <summary>
    /// Asynchronously gets all lights registered with the bridge.
    /// </summary>
    /// <returns>An enumerable of <see cref="Light"/>s registered with the bridge.</returns>
    public async Task<IEnumerable<Light>> GetLightsAsync()
    {
      CheckInitialized();

      HttpClient client = HueClient.GetHttpClient();
      string stringResult = await client.GetStringAsync(new Uri(String.Format("{0}lights", ApiBase))).ConfigureAwait(false);

      List<Light> results = new List<Light>();

      JToken token = JToken.Parse(stringResult);
      if (token.Type == JTokenType.Object)
      {
          //Each property is a light
          var jsonResult = (JObject)token;

          foreach (var prop in jsonResult.Properties())
          {
                  Light newLight = JsonConvert.DeserializeObject<Light>(prop.Value.ToString());
                  newLight.Id = prop.Name;
                  results.Add(newLight);
          }
      }
     return results;
    }

    /// <summary>
    /// Send a lightCommand to a list of lights
    /// </summary>
    /// <param name="command"></param>
    /// <param name="lightList">if null, send command to all lights</param>
    /// <returns></returns>
    public Task<HueResults> SendCommandAsync(LightCommand command, IEnumerable<string> lightList = null)
    {
      if (command == null)
        throw new ArgumentNullException("command");

      string jsonCommand = JsonConvert.SerializeObject(command, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

      return SendCommandRawAsync(jsonCommand, lightList);
    }


    /// <summary>
    /// Send a json command to a list of lights
    /// </summary>
    /// <param name="command"></param>
    /// <param name="lightList">if null, send command to all lights</param>
    /// <returns></returns>
    public async Task<HueResults> SendCommandRawAsync(string command, IEnumerable<string> lightList = null)
    {
      if (command == null)
        throw new ArgumentNullException("command");

      CheckInitialized();

      if (lightList == null || !lightList.Any())
      {
        //Group 0 always contains all the lights
        return await SendGroupCommandAsync(command).ConfigureAwait(false);
      }
      else
      {
        HueResults results = new HueResults();

        await lightList.ForEachAsync(_parallelRequests, async (lightId) =>
        {
          HttpClient client = HueClient.GetHttpClient();
          await client.PutAsync(new Uri(ApiBase + string.Format("lights/{0}/state", lightId)), new StringContent(command)).ConfigureAwait(false);

        }).ConfigureAwait(false);

        return results;
      }
    }


    /// <summary>
    /// Set the next Hue color
    /// </summary>
    /// <param name="lightList"></param>
    /// <returns></returns>
    public Task<HueResults> SetNextHueColorAsync(IEnumerable<string> lightList = null)
    {
      //Invalid JSON, but it works
      string command = "{\"hue\":+10000,\"sat\":255}";

      return SendCommandRawAsync(command, lightList);

    }


    /// <summary>
    /// Start searching for new lights
    /// </summary>
    /// <returns></returns>
    public async Task<HueResults> SearchNewLightsAsync(IEnumerable<string> deviceIds = null)
    {
      CheckInitialized();

      StringContent jsonStringContent = null;

      if(deviceIds != null)
      {
        dynamic jsonObj = new ExpandoObject();
        jsonObj.deviceid = deviceIds;

        string jsonString = JsonConvert.SerializeObject(jsonObj, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

        jsonStringContent = new StringContent(jsonString);

      }

      HttpClient client = HueClient.GetHttpClient();
      var response = await client.PostAsync(new Uri(String.Format("{0}lights", ApiBase)), jsonStringContent).ConfigureAwait(false);

      var jsonResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

      return DeserializeDefaultHueResult(jsonResult);

    }

    /// <summary>
    /// Gets a list of lights that were discovered the last time a search for new lights was performed. The list of new lights is always deleted when a new search is started.
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<Light>> GetNewLightsAsync()
    {
      CheckInitialized();

      HttpClient client = HueClient.GetHttpClient();
      string stringResult = await client.GetStringAsync(new Uri(String.Format("{0}lights/new", ApiBase))).ConfigureAwait(false);

#if DEBUG
      //stringResult = "{\"7\": {\"name\": \"Hue Lamp 7\"},   \"8\": {\"name\": \"Hue Lamp 8\"},    \"lastscan\": \"2012-10-29T12:00:00\"}";
#endif

      List<Light> results = new List<Light>();

      JToken token = JToken.Parse(stringResult);
      if (token.Type == JTokenType.Object)
      {
        //Each property is a light
        var jsonResult = (JObject)token;

        foreach(var prop in jsonResult.Properties())
        {
          if (prop.Name != "lastscan")
          {
            Light newLight = JsonConvert.DeserializeObject<Light>(prop.Value.ToString());
            newLight.Id = prop.Name;

            results.Add(newLight);

          }
        }
       
      }

      return results;

    }
  }
}
