#[derive(thiserror::Error, Debug)]
pub enum Error {
    #[error("Tauri error: {0}")]
    Tauri(#[from] tauri::Error),

    #[error("Eyre error: {0}")]
    Eyre(#[from] color_eyre::Report),

    #[error("Unknown error: {0}")]
    Unknown(String),
}

/// Catch-all: if an error that implements std::error::Error occurs
/// and that error does not have a variant in Error it will fallback
/// to be mapped to Unknown
impl From<Box<dyn std::error::Error>> for Error {
    fn from(error: Box<dyn std::error::Error>) -> Self {
        Error::Unknown(format!("{:?}", error))
    }
}

impl serde::Serialize for Error {
    fn serialize<S>(&self, serializer: S) -> core::result::Result<S::Ok, S::Error>
    where
        S: serde::ser::Serializer,
    {
        serializer.serialize_str(self.to_string().as_ref())
    }
}

pub type Result<T> = std::result::Result<T, Error>;

pub fn init() -> color_eyre::Result<()> {
    let (_panic_hook, eyre_hook) = color_eyre::config::HookBuilder::default()
        .panic_section(format!(
            "This is a bug. Consider reporting it at {}",
            env!("CARGO_PKG_REPOSITORY")
        ))
        .capture_span_trace_by_default(false)
        .display_env_section(false)
        .into_hooks();
    eyre_hook.install()?;
    std::panic::set_hook(Box::new(move |panic_info| {
        #[cfg(not(debug_assertions))]
        {
            use human_panic::{handle_dump, metadata, print_msg};
            let metadata = metadata!();
            let file_path = handle_dump(&metadata, panic_info);
            // prints human-panic message
            print_msg(file_path, &metadata)
                .expect("human-panic: printing error message to console failed");
            eprintln!("{}", _panic_hook.panic_report(panic_info)); // prints color-eyre stack trace to stderr
        }

        #[cfg(debug_assertions)]
        {
            // Better Panic stacktrace that is only enabled when debugging.
            better_panic::Settings::auto()
                .most_recent_first(false)
                .lineno_suffix(true)
                .verbosity(better_panic::Verbosity::Full)
                .create_panic_handler()(panic_info);
        }
    }));
    Ok(())
}
