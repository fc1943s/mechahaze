use std::env::VarError;
use std::path::{PathBuf};

    
fn get_home_path() -> Result<PathBuf, VarError> {
    return std::env::var("MECHAHAZE_HOME").map(PathBuf::from);
}

fn load(home_path: PathBuf) {
    println!("HOME_PATH: {:?}", home_path);

}

#[cfg(test)]
mod test {
    use super::*;

    #[test]
    fn test1() {
        let temp_dir = std::env::temp_dir();
        let home_path = temp_dir.join("test1");
        
        std::fs::create_dir_all(home_path.as_path()).unwrap();
        
        load(home_path.to_path_buf());
        
        std::fs::remove_dir_all(home_path.as_path()).unwrap();
        // panic!();
    }
}


fn main() {
    let home_path = get_home_path().unwrap();
    
    load(home_path);
    
}
