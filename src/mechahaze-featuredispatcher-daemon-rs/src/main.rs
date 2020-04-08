#![feature(dec2flt)]

mod x1;

use std::vec::Vec;
use std::fs::File;
use genawaiter::sync::Gen;
use byteorder::{ReadBytesExt, LittleEndian};
use itertools::Itertools;
use std::collections::HashMap;

use core::num::dec2flt::rawfp::RawFloat;
use std::path::Path;
use std::env;


trait FloatIterExt {
    fn float_min(&mut self) -> f32;
    fn float_max(&mut self) -> f32;
}

impl<T> FloatIterExt for T where T: Iterator<Item=f32> {
    fn float_min(&mut self) -> f32 {
        self.fold(f32::NAN, f32::min)
    }

    fn float_max(&mut self) -> f32 {
        self.fold(f32::NAN, f32::max)
    }
}





#[derive(Debug)]
struct WaveformHeaderV2 {
    flags: u32,
    sample_rate: i32,
    samples_per_pixel: i32,
    length: u32,
    channels: i32
}

#[derive(Debug)]
struct WaveformModel {
    version: i32,
    header: WaveformHeaderV2,
    channel_l: Vec<f32>,
    channel_r: Vec<f32>
}


const DEFAULT_WAVEFORM_VERSION: i32 = 2;

fn normalize (channels: HashMap<i32, Vec<f32>>) -> HashMap<i32, Vec<f32>> {
    
    let max: f32 = 
        channels
        .iter()
        .map(|(_, data)| data.iter().cloned().float_max())
        .float_max();
    
    let ratio = 100. / max;

    channels
    .iter()
    .map(|(&channel, data)| 
         (channel, data.iter().map(|&x| (x * ratio) / 100.).collect())
    )
    .collect()
}

fn tst ()  -> Result<WaveformModel, &'static str> {
    let root_path = env::var("1943_MECHAHAZE_PATH").unwrap();
    
    let path =
        Path::new(&root_path)
            .join("db-tracks")
            .join("!luuli - Molt - 11 Be Patient [id=20200104122241373]")
            .join("!luuli - Molt - 11 Be Patient [id=20200104122241373].all.peaks.levels.dat");
    
    let mut file = File::open(path).unwrap();
    
    let version = file.read_i32::<LittleEndian>().unwrap();
    
    if version != DEFAULT_WAVEFORM_VERSION {
//        Err(format!("Invalid waveform version: {}", version));
    }
    
    let flags = file.read_u32::<LittleEndian>().unwrap();
    let sample_rate = file.read_i32::<LittleEndian>().unwrap();
    let samples_per_pixel = file.read_i32::<LittleEndian>().unwrap();
    let length = file.read_u32::<LittleEndian>().unwrap();
    let channels = file.read_i32::<LittleEndian>().unwrap();
    
    let header = WaveformHeaderV2 { flags, sample_rate, samples_per_pixel, length, channels };
    
    let header_length = header.length;
    let header_channels = header.channels;

    let data : HashMap<i32, Vec<f32>> = 
        Gen::new(|co| async move {
            for _ in 0 .. header_length {
                for channel in 0 .. header_channels {
                    let max = file.read_u8().unwrap();
                    let min = file.read_u8().unwrap();
                    
                    let value = max as f32 - min as f32;

                    co.yield_((channel, value)).await;
                }
            }
        })
        .into_iter()
        .into_group_map();
    
    let data = normalize(data);
    
    let result = WaveformModel { 
        version, 
        header, 
        channel_l:data[&0].clone(), 
        channel_r:data[&1].clone() 
    };
    
    Ok(result)
}

fn main() {
    println!("Hello, world!");
    
    match tst() {
        Ok(v) => println!("SUCC: {:?} {:?}", v.header, v.channel_l.len()),
        Err(e) => println!("ERROR: {:?}", e)
    }
}
