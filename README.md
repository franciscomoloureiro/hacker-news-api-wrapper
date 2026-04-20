# hacker-news-api-wrapper

Sample integration wrapper for the Hacker News Firebase API.

 This is a proof of concept project and is not production ready.

---

## Operation Modes

The system supports two different data delivery strategies:

---

## 1. Server-Sent Events (SSE)

We use SSE to maintain long-lived connections to Hacker News.  
This reduces repeated polling pressure on the API and enables faster delivery of fresher data.

### Key characteristics:
- Persistent connection to Hacker News API
- Streaming updates instead of repeated polling
- Reduced request overhead on both client and server
- More responsive data delivery

### Current limitations:
- Exception handling needs improvement
- Reconnection logic is not fully robust
- Client lifecycle management can be improved
- Scaling concurrent clients is not fully optimized

### Summary:
This is the more complex approach, but also the most efficient and real-time capable solution.

---

## 2. HTTP Request / Response

This mode uses a standard request-response pattern.

### How it works:
- Data is cached for 30 seconds
- Requests for stale data trigger cache refresh
- Clients receive cached or freshly fetched data depending on state

### Characteristics:
- Simpler implementation compared to SSE
- Easier to integrate into traditional APIs
- Suitable for non-real-time use cases

### Current limitations:
- Stale data is unavoidable due to caching strategy
- High request volume may stress Hacker News API
- Needs better load handling under spikes

### Potential improvements:
- Fetch only required subsets (e.g., top N stories instead of full dataset)
- Implement smarter cache invalidation strategies
- Add circuit breaking for Hacker News API failures or latency spikes
- Introduce request coalescing to reduce duplicate upstream calls
