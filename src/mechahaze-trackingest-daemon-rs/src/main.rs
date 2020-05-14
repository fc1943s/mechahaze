/*
# Notes

- id format:
    - hashxxxxxxxx-mp3-320
    - hashxxxxxxxx-wav-32
*/

fn load(home_path: std::path::PathBuf) -> Result<(), std::io::Error> {
    let db_tracks_path = home_path.join("db-tracks");
    
    std::fs::create_dir_all(db_tracks_path.as_path())?;
    
    
    println!("HOME_PATH: {:?}", home_path);
    
    Ok(())
}

#[cfg(test)]
mod test {
    #[test]
    fn load() {
        let temp_dir = std::env::temp_dir();
        let home_path = temp_dir.join("test1");
        
        std::fs::create_dir_all(home_path.as_path()).unwrap();
        
        super::load(home_path.to_path_buf()).unwrap();
        
        std::fs::remove_dir_all(home_path.as_path()).unwrap();
    }
}

fn get_home_path() -> Result<std::path::PathBuf, std::env::VarError> {
    return std::env::var("MECHAHAZE_HOME").map(std::path::PathBuf::from);
}

fn main() {
    let home_path = get_home_path().unwrap();
    
    load(home_path).unwrap();
}
