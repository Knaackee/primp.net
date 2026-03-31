use serde::Serialize;
use std::env;
use std::time::Instant;

#[derive(Serialize)]
struct Metric {
    avg_ms: f64,
    min_ms: f64,
    max_ms: f64,
}

#[derive(Serialize)]
struct ResultPayload {
    client: &'static str,
    iterations: usize,
    warmup: usize,
    get: Metric,
    post_json: Metric,
}

fn parse_arg_usize(args: &[String], name: &str, default: usize) -> usize {
    args.windows(2)
        .find(|w| w[0] == name)
        .and_then(|w| w[1].parse::<usize>().ok())
        .unwrap_or(default)
}

fn to_metric(values: &[f64]) -> Metric {
    let sum: f64 = values.iter().sum();
    let avg = if values.is_empty() { 0.0 } else { sum / values.len() as f64 };
    let min = values.iter().copied().fold(f64::INFINITY, f64::min);
    let max = values.iter().copied().fold(f64::NEG_INFINITY, f64::max);

    Metric {
        avg_ms: avg,
        min_ms: if min.is_finite() { min } else { 0.0 },
        max_ms: if max.is_finite() { max } else { 0.0 },
    }
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let args: Vec<String> = env::args().collect();
    let iterations = parse_arg_usize(&args, "--iterations", 20);
    let warmup = parse_arg_usize(&args, "--warmup", 3);
    let base_url = env::var("PRIMP_BENCH_BASE_URL").unwrap_or_else(|_| "http://127.0.0.1:18080".to_string());

    let client = primp::Client::builder()
        .impersonate(primp::Impersonate::ChromeV146)
        .impersonate_os(primp::ImpersonateOS::Windows)
        .timeout(std::time::Duration::from_secs(30))
        .build()?;

    for _ in 0..warmup {
        let _ = client
            .get(format!("{}/get", base_url))
            .send()
            .await?;
        let _ = client
            .post(format!("{}/post", base_url))
            .header("content-type", "application/json")
            .body("{\"benchmark\":true}")
            .send()
            .await?;
    }

    let mut get_times = Vec::with_capacity(iterations);
    let mut post_times = Vec::with_capacity(iterations);

    for _ in 0..iterations {
        let start = Instant::now();
        let response = client
            .get(format!("{}/get", base_url))
            .send()
            .await?;
        if !response.status().is_success() {
            return Err(format!("GET failed with status {}", response.status()).into());
        }
        get_times.push(start.elapsed().as_secs_f64() * 1000.0);
    }

    for _ in 0..iterations {
        let start = Instant::now();
        let response = client
            .post(format!("{}/post", base_url))
            .header("content-type", "application/json")
            .body("{\"benchmark\":true}")
            .send()
            .await?;
        if !response.status().is_success() {
            return Err(format!("POST failed with status {}", response.status()).into());
        }
        post_times.push(start.elapsed().as_secs_f64() * 1000.0);
    }

    let payload = ResultPayload {
        client: "original-primp-rust",
        iterations,
        warmup,
        get: to_metric(&get_times),
        post_json: to_metric(&post_times),
    };

    println!("{}", serde_json::to_string_pretty(&payload)?);
    Ok(())
}
