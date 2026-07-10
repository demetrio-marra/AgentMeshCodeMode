# QMD Documentation
This document explains the three core search modes in QMD (lex, vec, and hyde), how they differ in behavior and use cases, and how they combine in the hybrid search pipeline. 

## Overview
QMD supports three typed search modes, each routing to a different retrieval backend. These can be triggered automatically via query expansion or manually via the structured query format 
src/store.ts
214-221

| Mode | Backend | Use Case | Query Style |
|------|--------|---------|------------|
| lex  | FTS5 BM25 | Keyword matching with boolean operators | "exact phrase" -exclude term1 term2 |
| vec  | Vector similarity (cosine) | Semantic understanding, paraphrases | Natural language question |
| hyde | Vector similarity (cosine) | Hypothetical answer embedding | Passage-style answer text |

Each mode produces a list of results with scores normalized to [0, 1]. When used together via query expansion, results are combined using Reciprocal Rank Fusion (RRF) and optionally reranked by an LLM

## Lex Mode: BM25 Keyword Search
### Purpose
Lex mode performs full-text keyword search using SQLite's FTS5 extension with BM25 scoring. It excels at exact term matching, technical identifiers (like function names), and queries where keyword presence is the primary signal.

### Query Syntax
Lex queries support boolean operators and phrase matching 
- **Exact phrases**: `"memory allocation"` matches the exact sequence 
- **Negation**: `performance -sports` excludes documents containing the term "sports" 
- **Prefix match**: `perf` matches "performance" 

### Examples

```
lex: CAP theorem consistency
lex: "machine learning" -"deep learning"
lex: auth -oauth -saml
```

## Vec Query Syntax

Vec queries are natural language questions. No special syntax — just write what you're looking for.

```
vec: how does the rate limiter handle burst traffic
vec: what is the tradeoff between consistency and availability
```

## Hyde Query Syntax

Hyde queries are hypothetical answer passages (50-100 words). Write what you expect the answer to look like.

```
hyde: The rate limiter uses a sliding window algorithm with a 60-second window. When a client exceeds 100 requests per minute, subsequent requests return 429 Too Many Requests.
```

## Multi-Line Queries

Combine multiple query types for best results. First query gets 2x weight in fusion.

```
lex: rate limiter algorithm
vec: how does rate limiting work in the API
hyde: The API implements rate limiting using a token bucket algorithm...
```

## Constraints

- `lex` syntax (`-term`, `"phrase"`) only works in lex queries
