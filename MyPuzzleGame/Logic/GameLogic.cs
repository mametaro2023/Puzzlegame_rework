        public List<Mino> GetNextMinos()
        {
            lock (_stateLock)
            {
                // Return a copy to avoid threading issues
                return new List<Mino>(_nextMinos);
            }
        }

        public IEnumerable<AnimatingBlock> GetFallingBlocks()
        {
            lock (_stateLock)
            {
                // Return a copy to prevent modification outside of the lock
                return new List<AnimatingBlock>(_fallingBlocks);
            }
        }