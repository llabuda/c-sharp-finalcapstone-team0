import axios from 'axios';

const http = axios.create({
    baseURL: "https://localhost:44315"
});

export default {
  
  getAnimals() { 
    return http.get(`/animals`);
  },

  getAnimal(id) { 
    return http.get(`/animals/${id}`)
  },

  addAnimal(animal) { 
    return http.post(`/animals`, animal)
  },

  update(id, animal) { 
    return http.put(`/animals/${id}`, animal)
  },

  delete(id) { 
    return http.delete(`/animals/${id}`)
  }


}
